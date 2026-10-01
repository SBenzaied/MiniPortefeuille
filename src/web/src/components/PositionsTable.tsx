import { useEffect, useMemo, useRef, useState } from 'react'
import type { Position } from '../api/types'
import { ASSET_CLASS_LABELS, formatMoney, formatNumber } from '../lib/format'
import { Icon } from './Icon'

export interface PositionRow extends Position {
  /** Valeur dans la devise de référence, fournie par l'endpoint de valorisation. */
  referenceValue: number
}

type SortKey = 'instrument' | 'assetClass' | 'quantity' | 'unitPrice' | 'currency' | 'marketValue' | 'referenceValue'
type Direction = 'asc' | 'desc'

interface Props {
  rows: PositionRow[]
  referenceCurrency: string
  /** Valeur totale dans la devise de référence, affichée en pied de tableau. */
  total?: number
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

export function PositionsTable({ rows, referenceCurrency, total, onEdit, onDelete }: Props) {
  const [sort, setSort] = useState<{ key: SortKey; direction: Direction }>({ key: 'referenceValue', direction: 'desc' })
  // Suppression confirmée dans la ligne : pas de boîte de dialogue du navigateur devant un client.
  const [confirming, setConfirming] = useState<number | null>(null)
  const deleteButtons = useRef(new Map<number, HTMLButtonElement>())
  const returnFocusTo = useRef<number | null>(null)

  useEffect(() => {
    if (confirming === null && returnFocusTo.current !== null) {
      deleteButtons.current.get(returnFocusTo.current)?.focus()
      returnFocusTo.current = null
    }
  }, [confirming])

  function cancel(id: number) {
    returnFocusTo.current = id
    setConfirming(null)
  }

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
          <Icon className="sort-indicator" name={active ? (sort.direction === 'asc' ? 'sortAsc' : 'sortDesc') : 'sortNone'} />
        </button>
      </th>
    )
  }

  if (rows.length === 0) {
    return <p className="empty">Ce client n'a encore aucune position. Ajoutez-en une pour calculer sa répartition.</p>
  }

  return (
    <div className="table-scroll">
      <table className="data-table positions-table">
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
            <tr key={p.id} className={confirming === p.id ? 'is-confirming' : undefined}>
              <td className="cell-strong p-inst">{p.instrument}</td>
              <td className="p-class">{ASSET_CLASS_LABELS[p.assetClass]}</td>
              <td className="num p-qty">{formatNumber(p.quantity)}</td>
              <td className="num p-price">{formatNumber(p.unitPrice, 2)}</td>
              <td className="p-cur">{p.currency}</td>
              <td className="num p-mv">{formatMoney(p.marketValue, p.currency)}</td>
              <td className="num p-ref">{formatMoney(p.referenceValue, referenceCurrency)}</td>
              <td className="row-actions p-actions">
                {confirming === p.id ? (
                  <span className="row-actions-inner confirm" role="group" aria-label={`Supprimer ${p.instrument} ?`}>
                    <span className="confirm-question">Supprimer ?</span>
                    <button
                      type="button"
                      className="confirm-button"
                      autoFocus
                      onClick={() => {
                        setConfirming(null)
                        onDelete(p)
                      }}
                      aria-label={`Confirmer la suppression de ${p.instrument}`}
                    >
                      Confirmer
                    </button>
                    <button type="button" className="text-button" onClick={() => cancel(p.id)}>
                      Annuler
                    </button>
                  </span>
                ) : (
                  <span className="row-actions-inner">
                    <button type="button" className="text-button" onClick={() => onEdit(p)} aria-label={`Modifier ${p.instrument}`}>
                      Modifier
                    </button>
                    <button
                      type="button"
                      className="delete-button"
                      ref={(el) => {
                        if (el) deleteButtons.current.set(p.id, el)
                        else deleteButtons.current.delete(p.id)
                      }}
                      onClick={() => setConfirming(p.id)}
                      aria-label={`Supprimer ${p.instrument}`}
                    >
                      Supprimer
                    </button>
                  </span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
        {total !== undefined && (
          <tfoot>
            <tr className="table-total">
              <td colSpan={6}>Total</td>
              <td className="num">{formatMoney(total, referenceCurrency)}</td>
              <td className="p-actions" />
            </tr>
          </tfoot>
        )}
      </table>
    </div>
  )
}
