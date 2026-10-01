import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import type { Allocation, AssetClass } from '../api/types'
import { ASSET_CLASS_LABELS, formatMoney, formatPercent } from '../lib/format'

// Une couleur fixe par classe d'actifs (jamais selon le rang) : définie en CSS pour le mode sombre.
const COLORS: Record<AssetClass, string> = {
  Equities: 'var(--series-1)',
  Bonds: 'var(--series-2)',
  Cash: 'var(--series-3)',
  Other: 'var(--series-4)',
}

interface Props {
  allocations: Allocation[]
  currency: string
}

/**
 * Répartition par classe d'actifs : anneau + légende chiffrée. La légende fait office de vue
 * tabulaire, l'information n'est donc jamais portée par la couleur seule.
 */
export function AllocationChart({ allocations, currency }: Props) {
  const visible = allocations.filter((a) => a.amount > 0)

  if (visible.length === 0) {
    return <p className="muted">Aucune position : pas de répartition à afficher.</p>
  }

  return (
    <div className="allocation">
      <div className="allocation-chart" aria-hidden="true">
        <ResponsiveContainer width="100%" height={200}>
          <PieChart>
            <Pie
              data={visible}
              dataKey="amount"
              nameKey="assetClass"
              innerRadius="62%"
              outerRadius="100%"
              stroke="var(--surface)"
              strokeWidth={2}
              isAnimationActive={false}
            >
              {visible.map((a) => (
                <Cell key={a.assetClass} fill={COLORS[a.assetClass]} />
              ))}
            </Pie>
            <Tooltip
              formatter={(value, _name, item) => [
                `${formatMoney(Number(value), currency)} · ${formatPercent((item.payload as Allocation).percentage)}`,
                ASSET_CLASS_LABELS[(item.payload as Allocation).assetClass],
              ]}
              contentStyle={{ background: 'var(--surface-raised)', border: '1px solid var(--border)', borderRadius: 6 }}
              itemStyle={{ color: 'var(--text)' }}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>

      <table className="allocation-legend">
        <caption className="visually-hidden">Répartition par classe d'actifs</caption>
        <thead>
          <tr>
            <th scope="col">Classe d'actifs</th>
            <th scope="col" className="num">Montant</th>
            <th scope="col" className="num">Part</th>
          </tr>
        </thead>
        <tbody>
          {allocations.map((a) => (
            <tr key={a.assetClass}>
              <th scope="row">
                <span className="swatch" style={{ background: COLORS[a.assetClass] }} />
                {ASSET_CLASS_LABELS[a.assetClass]}
              </th>
              <td className="num">{formatMoney(a.amount, currency)}</td>
              <td className="num">{formatPercent(a.percentage)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
