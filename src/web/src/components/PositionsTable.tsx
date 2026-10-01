import { useMemo, useState } from 'react'
import type { Position } from '../api/types'
import { ASSET_CLASS_LABELS, formatMoney, formatNumber } from '../lib/format'

export interface PositionRow extends Position {
  /** Valeur dans la devise de référence, fournie par l'endpoint de valorisation. */
  referenceValue: number
}

type SortKey = 'instrument' | 'assetClass' | 'quantity' | 'unitPrice' | 'currency' | 'marketValue' | 'referenceValue'
type Direction = 'asc' | 'desc'

interface Props {
  rows: PositionRow[]
  referenceCurrency: string
  onEdit: (position: Position) => void
  onDelete: (position: Position) => void
}

const collator = new Intl.Collator('fr', { sensitivity: 'base' })

function compare(a: PositionRow, b: PositionRow, key: SortKey): number {
  switch (key) {
    case 'instrument':
    case 'currency':
      return collator.compare(a[key], b[key])
    case 'assetClass':
      return collator.compare(ASSET_CLASS_LABELS[a.assetClass], ASSET_CLASS_LABELS[b.assetClass])
    default:
      return a[key] - b[key]
  }
}

export function PositionsTable({ rows, referenceCurrency, onEdit, onDelete }: Props) {
  const [sort, setSort] = useState<{ key: SortKey; direction: Direction }>({ key: 'referenceValue', direction: 'desc' })

  const sorted = useMemo(() => {
    const factor = sort.direction === 'asc' ? 1 : -1
    // Tri stable avec l'instrument en critère secondaire pour un ordre déterministe.
    return [...rows].sort((a, b) => factor * compare(a, b, sort.key) || collator.compare(a.instrument, b.instrument))
  }, [rows, sort])

  function toggle(key: SortKey) {
    setSort((current) =>
      current.key === key
        ? { key, direction: current.direction === 'asc' ? 'desc' : 'asc' }
        : { key, direction: key === 'instrument' || key === 'assetClass' || key === 'currency' ? 'asc' : 'desc' },
    )
  }

  function header(key: SortKey, label: string, numeric = false) {
    const active = sort.key === key
    return (
      <th
        scope="col"
        className={numeric ? 'num' : undefined}
        aria-sort={active ? (sort.direction === 'asc' ? 'ascending' : 'descending') : 'none'}
      >
        <button type="button" className="sort-button" onClick={() => toggle(key)}>
          {label}
          <span aria-hidden="true" className="sort-indicator">
            {active ? (sort.direction === 'asc' ? '▲' : '▼') : '↕'}
          </span>
        </button>
      </th>
    )
  }

  if (rows.length === 0) {
    return <p className="muted">Ce client n'a encore aucune position.</p>
  }

  return (
    <div className="table-scroll">
      <table className="data-table">
        <thead>
          <tr>
            {header('instrument', 'Instrument')}
            {header('assetClass', 'Classe')}
            {header('quantity', 'Quantité', true)}
            {header('unitPrice', 'Prix unitaire', true)}
            {header('currency', 'Devise')}
            {header('marketValue', 'Valeur (devise)', true)}
            {header('referenceValue', `Valeur ${referenceCurrency}`, true)}
            <th scope="col">
              <span className="visually-hidden">Actions</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {sorted.map((p) => (
            <tr key={p.id}>
              <td>{p.instrument}</td>
              <td>{ASSET_CLASS_LABELS[p.assetClass]}</td>
              <td className="num">{formatNumber(p.quantity)}</td>
              <td className="num">{formatNumber(p.unitPrice, 2)}</td>
              <td>{p.currency}</td>
              <td className="num">{formatMoney(p.marketValue, p.currency)}</td>
              <td className="num">{formatMoney(p.referenceValue, referenceCurrency)}</td>
              <td className="row-actions">
                <button type="button" className="link-button" onClick={() => onEdit(p)} aria-label={`Modifier ${p.instrument}`}>
                  Modifier
                </button>
                <button type="button" className="link-button danger" onClick={() => onDelete(p)} aria-label={`Supprimer ${p.instrument}`}>
                  Supprimer
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
