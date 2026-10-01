/** Erreur HTTP normalisée à partir d'une réponse ProblemDetails de l'API. */
export class ApiError extends Error {
  readonly status: number
  /** Erreurs de validation par champ (clés camelCase, comme dans le JSON envoyé). */
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, title: string, fieldErrors: Record<string, string[]> = {}) {
    super(title)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

interface ProblemDetails {
  title?: string
  errors?: Record<string, string[]>
}

const DEFAULT_MESSAGES: Record<number, string> = {
  401: 'Votre session a expiré. Veuillez vous reconnecter.',
  403: "Vous n'avez pas accès à cette ressource.",
  404: 'Ressource introuvable.',
  429: 'Trop de tentatives. Réessayez dans une minute.',
}

let tokenProvider: () => string | null = () => null
let onUnauthorized: () => void = () => {}

/** Branche le client HTTP sur l'état d'authentification (appelé une fois par AuthProvider). */
export function configureHttp(options: { getToken: () => string | null; onUnauthorized: () => void }) {
  tokenProvider = options.getToken
  onUnauthorized = options.onUnauthorized
}

/** Téléchargement authentifié : un simple lien <a href> n'enverrait pas l'en-tête Authorization. */
export async function download(path: string, fileName: string): Promise<void> {
  const token = tokenProvider()
  const response = await fetch(path, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
  if (!response.ok) {
    const problem = await readProblem(response)
    if (response.status === 401 && token) onUnauthorized()
    throw new ApiError(response.status, problem.title ?? DEFAULT_MESSAGES[response.status] ?? 'Le téléchargement a échoué.')
  }
  const url = URL.createObjectURL(await response.blob())
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}

export async function request<T>(path: string, init: { method?: string; body?: unknown } = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  const token = tokenProvider()
  if (token) headers.Authorization = `Bearer ${token}`
  if (init.body !== undefined) headers['Content-Type'] = 'application/json'

  let response: Response
  try {
    response = await fetch(path, {
      method: init.method ?? 'GET',
      headers,
      body: init.body === undefined ? undefined : JSON.stringify(init.body),
    })
  } catch {
    throw new ApiError(0, "Impossible de joindre le serveur. Vérifiez votre connexion.")
  }

  if (response.ok) {
    return (response.status === 204 ? undefined : await response.json()) as T
  }

  const problem = await readProblem(response)
  // Un 401 sur une requête authentifiée signifie un jeton expiré ou révoqué.
  if (response.status === 401 && token) onUnauthorized()

  throw new ApiError(
    response.status,
    problem.title ?? DEFAULT_MESSAGES[response.status] ?? 'Une erreur inattendue est survenue.',
    problem.errors ?? {},
  )
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return {}
  }
}
