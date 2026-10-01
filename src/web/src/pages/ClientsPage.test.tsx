import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/portfolio'
import type { ClientSummary } from '../api/types'
import { ClientsPage } from './ClientsPage'

vi.mock('../api/portfolio', () => ({ api: { listClients: vi.fn() } }))

const clients: ClientSummary[] = [
  { id: 1, name: 'Famille Dubois', riskProfile: 'Prudent', positionCount: 4, totalValueChf: 92488.5, alertCount: 0, alertAssetClasses: [] },
  { id: 2, name: 'Jean Rochat', riskProfile: 'Prudent', positionCount: 4, totalValueChf: 73510, alertCount: 1, alertAssetClasses: ['Equities'] },
  { id: 4, name: 'Marc Weber', riskProfile: 'Dynamic', positionCount: 3, totalValueChf: 224600, alertCount: 2, alertAssetClasses: ['Equities', 'Cash'] },
]

describe('ClientsPage', () => {
  beforeEach(() => {
    vi.mocked(api.listClients).mockReset()
    vi.mocked(api.listClients).mockImplementation(async (profile) =>
      profile ? clients.filter((c) => c.riskProfile === profile) : clients,
    )
  })

  it('liste les clients avec leur valeur et leurs alertes', async () => {
    render(<ClientsPage />, { wrapper: MemoryRouter })

    expect(await screen.findByRole('link', { name: 'Jean Rochat' })).toHaveAttribute('href', '/clients/2')
    expect(screen.getByText('1 alerte')).toBeInTheDocument()
    expect(screen.getByText('2 alertes')).toBeInTheDocument()
    expect(screen.getByText('Actions, Liquidités')).toBeInTheDocument()
    expect(api.listClients).toHaveBeenCalledWith(undefined)
  })

  it('filtre par profil de risque via l’API', async () => {
    const user = userEvent.setup()
    render(<ClientsPage />, { wrapper: MemoryRouter })
    await screen.findByText('Marc Weber')

    await user.click(screen.getByRole('radio', { name: 'Dynamique' }))

    expect(api.listClients).toHaveBeenLastCalledWith('Dynamic')
    expect(await screen.findByText('Marc Weber')).toBeInTheDocument()
    expect(screen.queryByText('Jean Rochat')).not.toBeInTheDocument()
  })

  it('affiche l’erreur de l’API', async () => {
    const { ApiError } = await import('../api/http')
    vi.mocked(api.listClients).mockRejectedValue(new ApiError(0, 'Impossible de joindre le serveur.'))

    render(<ClientsPage />, { wrapper: MemoryRouter })

    expect(await screen.findByRole('alert')).toHaveTextContent('Impossible de joindre le serveur.')
  })
})
