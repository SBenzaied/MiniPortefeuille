import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { RequireAuth } from './auth/RequireAuth'
import { Layout } from './components/Layout'
import { ClientsPage } from './pages/ClientsPage'
import { LoginPage } from './pages/LoginPage'

// La fiche client embarque la librairie de graphiques : chargée à la demande.
const ClientPage = lazy(() => import('./pages/ClientPage').then((m) => ({ default: m.ClientPage })))

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            element={
              <RequireAuth>
                <Layout />
              </RequireAuth>
            }
          >
            <Route path="/clients" element={<ClientsPage />} />
            <Route
              path="/clients/:clientId"
              element={
                <Suspense fallback={<p className="muted">Chargement…</p>}>
                  <ClientPage />
                </Suspense>
              }
            />
          </Route>
          <Route path="*" element={<Navigate to="/clients" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
