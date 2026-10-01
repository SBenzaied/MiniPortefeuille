import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { Allocation, AllocationAlert, AllocationLimit } from '../api/types'
import { describeLimit } from '../lib/format'
import { AllocationBands } from './AllocationBands'

const allocations: Allocation[] = [
  { assetClass: 'Equities', amount: 48330, percentage: 65.75 },
  { assetClass: 'Bonds', amount: 15180, percentage: 20.65 },
  { assetClass: 'Cash', amount: 10000, percentage: 13.6 },
  { assetClass: 'Other', amount: 0, percentage: 0 },
]
const limits: AllocationLimit[] = [
  { assetClass: 'Equities', minPercentage: null, maxPercentage: 30 },
  { assetClass: 'Other', minPercentage: null, maxPercentage: 10 },
]
const alerts: AllocationAlert[] = [
  { assetClass: 'Equities', kind: 'AboveMaximum', actualPercentage: 65.75, limitPercentage: 30, message: '…' },
]

describe('AllocationBands', () => {
  it('décrit les bornes du profil en texte', () => {
    expect(describeLimit({ assetClass: 'Equities', minPercentage: 20, maxPercentage: 60 })).toBe('20 à 60 %')
    expect(describeLimit({ assetClass: 'Equities', minPercentage: null, maxPercentage: 30 })).toBe('≤ 30 %')
    expect(describeLimit({ assetClass: 'Equities', minPercentage: 50, maxPercentage: null })).toBe('≥ 50 %')
    expect(describeLimit(undefined)).toBe('Sans borne')
  })

  it('signale la classe hors profil sans s’appuyer sur la couleur', () => {
    render(<AllocationBands allocations={allocations} limits={limits} alerts={alerts} currency="CHF" />)

    const rows = within(screen.getByRole('list', { name: "Répartition par classe d'actifs" })).getAllByRole('listitem')
    expect(rows).toHaveLength(4)
    expect(rows[0]).toHaveTextContent(/Actions.*≤ 30 % · , écart de \+35\.75 pts.*\(hors profil\).*CHF 48.330\.00.*65\.75 %/)
    expect(rows[1]).not.toHaveTextContent('hors profil')
  })

  it('affiche un état vide pour un portefeuille sans position', () => {
    const empty = allocations.map((a) => ({ ...a, amount: 0, percentage: 0 }))
    render(<AllocationBands allocations={empty} limits={limits} alerts={[]} currency="CHF" />)
    expect(screen.getByText(/Aucune position/)).toBeInTheDocument()
  })
})
