import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { PositionsTable, type PositionRow } from './PositionsTable'

const rows: PositionRow[] = [
  { id: 1, instrument: 'Nestlé SA', assetClass: 'Equities', quantity: 10, unitPrice: 90, currency: 'CHF', marketValue: 900, referenceValue: 900 },
  { id: 2, instrument: 'Apple Inc.', assetClass: 'Equities', quantity: 5, unitPrice: 200, currency: 'USD', marketValue: 1000, referenceValue: 800 },
  { id: 3, instrument: 'Bund 2034', assetClass: 'Bonds', quantity: 20, unitPrice: 100, currency: 'EUR', marketValue: 2000, referenceValue: 1880 },
]

function instrumentsInOrder() {
  const body = screen.getAllByRole('rowgroup')[1]
  return within(body)
    .getAllByRole('row')
    .map((row) => within(row).getAllByRole('cell')[0].textContent)
}

function renderTable() {
  render(<PositionsTable rows={rows} referenceCurrency="CHF" onEdit={vi.fn()} onDelete={vi.fn()} />)
}

describe('PositionsTable', () => {
  it('trie par valeur en devise de référence décroissante par défaut', () => {
    renderTable()

    expect(instrumentsInOrder()).toEqual(['Bund 2034', 'Nestlé SA', 'Apple Inc.'])
    expect(screen.getByRole('columnheader', { name: /Valeur CHF/ })).toHaveAttribute('aria-sort', 'descending')
  })

  it('trie par une autre colonne au clic, puis inverse au second clic', async () => {
    const user = userEvent.setup()
    renderTable()

    await user.click(screen.getByRole('button', { name: /Instrument/ }))
    expect(instrumentsInOrder()).toEqual(['Apple Inc.', 'Bund 2034', 'Nestlé SA'])
    expect(screen.getByRole('columnheader', { name: /Instrument/ })).toHaveAttribute('aria-sort', 'ascending')

    await user.click(screen.getByRole('button', { name: /Instrument/ }))
    expect(instrumentsInOrder()).toEqual(['Nestlé SA', 'Bund 2034', 'Apple Inc.'])
  })

  it('trie les colonnes numériques numériquement, pas alphabétiquement', async () => {
    const user = userEvent.setup()
    renderTable()

    await user.click(screen.getByRole('button', { name: /Quantité/ })) // premier clic : décroissant
    expect(instrumentsInOrder()).toEqual(['Bund 2034', 'Nestlé SA', 'Apple Inc.'])
  })

  it('transmet la position à modifier ou supprimer', async () => {
    const user = userEvent.setup()
    const onEdit = vi.fn()
    const onDelete = vi.fn()
    render(<PositionsTable rows={rows} referenceCurrency="CHF" onEdit={onEdit} onDelete={onDelete} />)

    await user.click(screen.getByRole('button', { name: 'Modifier Apple Inc.' }))
    await user.click(screen.getByRole('button', { name: 'Supprimer Nestlé SA' }))

    expect(onEdit).toHaveBeenCalledWith(expect.objectContaining({ id: 2 }))
    expect(onDelete).toHaveBeenCalledWith(expect.objectContaining({ id: 1 }))
  })

  it('affiche un message quand il n’y a aucune position', () => {
    render(<PositionsTable rows={[]} referenceCurrency="CHF" onEdit={vi.fn()} onDelete={vi.fn()} />)

    expect(screen.getByText(/aucune position/i)).toBeInTheDocument()
  })
})
