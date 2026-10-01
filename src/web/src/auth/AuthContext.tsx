import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { configureHttp } from '../api/http'
import { api } from '../api/portfolio'
import type { Advisor } from '../api/types'

interface Session {
  token: string
  expiresAt: string
  advisor: Advisor
}

interface AuthState {
  advisor: Advisor | null
  login: (userName: string, password: string) => Promise<void>
  logout: () => void
}

const STORAGE_KEY = 'mini-portefeuille.session'
const AuthContext = createContext<AuthState | null>(null)

// sessionStorage plutôt que localStorage : la session ne survit pas à la fermeture de l'onglet.
// Limite connue : un jeton lisible en JavaScript reste exposé en cas de faille XSS (voir README).
function loadSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const session = JSON.parse(raw) as Session
    return new Date(session.expiresAt) > new Date() ? session : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(loadSession)

  const logout = useCallback(() => {
    sessionStorage.removeItem(STORAGE_KEY)
    setSession(null)
  }, [])

  const login = useCallback(async (userName: string, password: string) => {
    const response = await api.login(userName, password)
    const next = { token: response.accessToken, expiresAt: response.expiresAt, advisor: response.advisor }
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    setSession(next)
  }, [])

  // Configuré pendant le rendu (et non dans un effet) pour que les requêtes lancées par les
  // effets des composants enfants disposent déjà du jeton.
  configureHttp({ getToken: () => session?.token ?? null, onUnauthorized: logout })

  // Déconnexion automatique à l'expiration du jeton.
  useEffect(() => {
    if (!session) return
    const delay = new Date(session.expiresAt).getTime() - Date.now()
    const timer = setTimeout(logout, Math.max(delay, 0))
    return () => clearTimeout(timer)
  }, [session, logout])

  const value = useMemo(() => ({ advisor: session?.advisor ?? null, login, logout }), [session, login, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react/only-export-components -- hook associé au provider
export function useAuth(): AuthState {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth doit être utilisé dans un AuthProvider.')
  return context
}
