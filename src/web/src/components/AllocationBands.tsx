import type { CSSProperties } from 'react'
import type { Allocation, AllocationAlert, AllocationLimit, AssetClass } from '../api/types'
import { ASSET_CLASS_LABELS, describeLimit, formatGap, formatMoney, formatPercent } from '../lib/format'

// Une couleur fixe par classe d'actifs (jamais selon le rang) : définie en CSS pour le mode sombre.
const SERIES_COLORS: Record<AssetClass, string> = {
  Equities: 'var(--series-equities)',
  Bonds: 'var(--series-bonds)',
  Cash: 'var(--series-cash)',
  Other: 'var(--series-other)',
}

interface Props {
  allocations: Allocation[]
  limits: AllocationLimit[]
  alerts: AllocationAlert[]
  currency: string
}

/**
 * Répartition face au profil : une ligne par classe d'actifs, à l'échelle exacte sur un axe
 * 0–100 % commun. La bande teintée est la plage autorisée par le profil ; la part qui en sort
 * est hachurée. Chaque ligne porte aussi ses chiffres et sa borne en texte : la couleur n'est
 * jamais la seule information.
 */
export function AllocationBands({ allocations, limits, alerts, currency }: Props) {
  if (!allocations.some((a) => a.amount > 0)) {
    return <p className="empty">Aucune position : pas de répartition à afficher.</p>
  }

  return (
    <>
      <div className="committee-scale" aria-hidden="true">
        <span>Classe d'actifs</span>
        <div className="scale-ticks">
          <span style={{ left: '0%' }}>0 %</span>
          <span style={{ left: '25%' }}>25 %</span>
          <span style={{ left: '50%' }}>50 %</span>
          <span style={{ left: '75%' }}>75 %</span>
          <span style={{ left: '100%' }}>100 %</span>
        </div>
        <span>Profil</span>
        <span className="num">Montant</span>
        <span className="num">Part</span>
      </div>
      <ul className="committee-grid" aria-label="Répartition par classe d'actifs">
        {allocations.map((a, i) => {
          const limit = limits.find((l) => l.assetClass === a.assetClass)
          const min = limit?.minPercentage ?? null
          const max = limit?.maxPercentage ?? null
          const breach = alerts.some((alert) => alert.assetClass === a.assetClass)
          const share = Math.min(a.percentage, 100)
          // Écart à la borne franchie, calculé sur le pourcentage arrondi que compare l'alerte.
          const gap =
            max !== null && a.percentage > max ? a.percentage - max : min !== null && a.percentage < min ? a.percentage - min : null
          const color = SERIES_COLORS[a.assetClass]
          const delay = { '--i': i } as CSSProperties

          return (
            <li key={a.assetClass} className={`committee-row${breach ? ' is-breach' : ''}`}>
              <span className="committee-class">
                <span className="series-key" style={{ background: color }} aria-hidden="true" />
                {ASSET_CLASS_LABELS[a.assetClass]}
              </span>
              <div className="track" aria-hidden="true">
                {limit && (min !== null || max !== null) && (
                  <span
                    className={`corridor${min !== null ? ' has-min' : ''}${max !== null ? ' has-max' : ''}`}
                    style={{ left: `${min ?? 0}%`, width: `${(max ?? 100) - (min ?? 0)}%` }}
                  />
                )}
                {share > 0 && <span className="bar" style={{ width: `${share}%`, background: color, ...delay }} />}
                {breach && max !== null && share > max && (
                  <span className="overshoot over" style={{ left: `${max}%`, width: `${share - max}%`, ...delay }} />
                )}
                {breach && min !== null && share < min && (
                  <span className="overshoot" style={{ left: `${share}%`, width: `${min - share}%`, ...delay }} />
                )}
              </div>
              <span className="committee-limit">
                <span className="visually-hidden">Profil : </span>
                {describeLimit(limit)}
                {breach && gap !== null && (
                  <span className="committee-gap">
                    <span aria-hidden="true"> · </span>
                    <span className="visually-hidden">, écart de </span>
                    {formatGap(gap)}
                  </span>
                )}
                {breach && <span className="visually-hidden"> (hors profil)</span>}
              </span>
              <span className="committee-amount">{formatMoney(a.amount, currency)}</span>
              <span className="committee-share">{formatPercent(a.percentage)}</span>
            </li>
          )
        })}
      </ul>
    </>
  )
}
