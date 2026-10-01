import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../auth/AuthContext'
import { LoginPage } from './LoginPage'

function renderLogin() {
  render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/clients" element={<p>Liste des clients</p>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  )
}

function jsonResponse(status: number, body: unknown) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('LoginPage', () => {
  it('affiche le message de l’API quand les identifiants sont refusés', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(401, { title: 'Identifiant ou mot de passe incorrect.', status: 401 })))
    const user = userEvent.setup()
    renderLogin()

    await user.type(screen.getByLabelText('Identifiant'), 'amartin')
    await user.type(screen.getByLabelText('Mot de passe'), 'faux')
    await user.click(screen.getByRole('button', { name: 'Se connecter' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Identifiant ou mot de passe incorrect.')
  })

  it('redirige vers la liste des clients après connexion', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse(200, {
        accessToken: 'jeton',
        expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
        advisor: { id: 1, userName: 'amartin', displayName: 'Alice Martin' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()
    renderLogin()

    await user.type(screen.getByLabelText('Identifiant'), 'amartin')
    await user.type(screen.getByLabelText('Mot de passe'), 'Demo-2026!')
    await user.click(screen.getByRole('button', { name: 'Se connecter' }))

    expect(await screen.findByText('Liste des clients')).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/login', expect.objectContaining({ method: 'POST' }))
  })
})
