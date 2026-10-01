import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { BrandMark, Icon } from './Icon'

export function Layout() {
  const { advisor, logout } = useAuth()

  return (
    <div className="app">
      <header className="app-header">
        <div className="app-header-inner">
          <Link to="/clients" className="brand">
            <BrandMark />
            Mini-portefeuille
          </Link>
          {advisor && (
            <div className="session">
              <span className="session-name">{advisor.displayName}</span>
              <button type="button" className="button on-band" onClick={logout}>
                <Icon name="logout" />
                Se déconnecter
              </button>
            </div>
          )}
        </div>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
