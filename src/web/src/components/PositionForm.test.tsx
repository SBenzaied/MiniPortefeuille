import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/http'
import type { Currency } from '../api/types'
import { PositionForm } from './PositionForm'

const currencies: Currency[] = [
  { code: 'CHF', name: 'Franc suisse', rateToChf: 1 },
  { code: 'EUR', name: 'Euro', rateToChf: 0.94 },
]

describe('PositionForm', () => {
  it('envoie les valeurs saisies, nombres convertis (virgule décimale acceptée)', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<PositionForm currencies={currencies} onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.type(screen.getByLabelText('Instrument'), 'Swisscom AG')
    await user.selectOptions(screen.getByLabelText("Classe d'actifs"), 'Equities')
    await user.type(screen.getByLabelText('Quantité'), '12')
    await user.type(screen.getByLabelText('Prix unitaire'), '520,5')
    await user.selectOptions(screen.getByLabelText('Devise'), 'EUR')
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }))

    expect(onSubmit).toHaveBeenCalledWith({
      instrument: 'Swisscom AG',
      assetClass: 'Equities',
      quantity: 12,
      unitPrice: 520.5,
      currency: 'EUR',
    })
  })

  it('affiche sous chaque champ les erreurs de validation renvoyées par l’API', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockRejectedValue(
      new ApiError(400, 'Les données envoyées ne sont pas valides.', {
        quantity: ['La quantité doit être strictement positive.'],
        currency: ['Devise inconnue. Devises acceptées : CHF, EUR, USD.'],
      }),
    )
    render(<PositionForm currencies={currencies} onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.type(screen.getByLabelText('Quantité'), '-3')
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Les données envoyées ne sont pas valides.')
    const quantity = screen.getByLabelText('Quantité')
    expect(quantity).toHaveAttribute('aria-invalid', 'true')
    expect(quantity).toHaveAccessibleDescription('La quantité doit être strictement positive.')
    expect(screen.getByLabelText('Devise')).toHaveAccessibleDescription(/Devise inconnue/)
  })

  it('efface l’erreur d’un champ dès qu’il est modifié', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockRejectedValue(new ApiError(400, 'Invalide', { instrument: ["L'instrument est obligatoire."] }))
    render(<PositionForm currencies={currencies} onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: 'Enregistrer' }))
    expect(await screen.findByText("L'instrument est obligatoire.")).toBeInTheDocument()

    await user.type(screen.getByLabelText('Instrument'), 'X')
    expect(screen.queryByText("L'instrument est obligatoire.")).not.toBeInTheDocument()
  })

  it('signale un texte non numérique sans appeler l’API', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    render(<PositionForm currencies={currencies} onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.type(screen.getByLabelText('Quantité'), 'douze')
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(screen.getByLabelText('Quantité')).toHaveAccessibleDescription(/Saisissez un nombre/)
  })

  it('pré-remplit le formulaire en modification', () => {
    render(
      <PositionForm
        position={{ id: 7, instrument: 'Bund 2034', assetClass: 'Bonds', quantity: 250, unitPrice: 97.1, currency: 'EUR', marketValue: 24275 }}
        currencies={currencies}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Modifier la position' })).toBeInTheDocument()
    expect(screen.getByLabelText('Instrument')).toHaveValue('Bund 2034')
    expect(screen.getByLabelText('Quantité')).toHaveValue('250')
    expect(screen.getByLabelText('Devise')).toHaveValue('EUR')
  })
})
