import { useCallback, useEffect, useState } from 'react'
import { ApiError } from '../api/http'

export type AsyncState<T> =
  | { status: 'loading'; data?: T }
  | { status: 'success'; data: T }
  | { status: 'error'; error: ApiError; data?: T }

type Settled<T> = { request: object; data?: T; error?: ApiError }

/**
 * Charge une donnée et expose son état. `load` doit être stable (useCallback) : il est relancé
 * quand il change. L'état « chargement » est dérivé (la dernière réponse correspond-elle à la
 * requête courante ?) plutôt que posé dans l'effet, et les réponses périmées sont ignorées.
 * La dernière donnée reste disponible pendant un rechargement pour éviter le clignotement.
 */
export function useAsync<T>(load: () => Promise<T>): AsyncState<T> & { reload: () => void } {
  const [request, setRequest] = useState<{ load: () => Promise<T> }>(() => ({ load }))
  const [settled, setSettled] = useState<Settled<T> | null>(null)

  // Nouvelle fonction de chargement → nouvelle requête (ajustement d'état pendant le rendu).
  if (request.load !== load) {
    setRequest({ load })
  }

  useEffect(() => {
    let cancelled = false
    request.load().then(
      (data) => !cancelled && setSettled({ request, data }),
      (error: unknown) =>
        !cancelled &&
        setSettled((previous) => ({
          request,
          data: previous?.data,
          error: error instanceof ApiError ? error : new ApiError(0, 'Une erreur inattendue est survenue.'),
        })),
    )
    return () => {
      cancelled = true
    }
  }, [request])

  const reload = useCallback(() => setRequest((current) => ({ load: current.load })), [])

  if (!settled || settled.request !== request) return { status: 'loading', data: settled?.data, reload }
  if (settled.error) return { status: 'error', error: settled.error, data: settled.data, reload }
  return { status: 'success', data: settled.data as T, reload }
}
