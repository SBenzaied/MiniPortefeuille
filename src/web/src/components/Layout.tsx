import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function Layout() {
  const { advisor, logout } = useAuth()

  return (
    <div className="app">
      <header className="app-header">
        <Link to="/clients" className="brand">
          Mini-portefeuille
        </Link>
        {advisor && (
          <div className="session">
            <span>{advisor.displayName}</span>
            <button type="button" className="secondary small" onClick={logout}>
              Se déconnecter
            </button>
          </div>
        )}
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
