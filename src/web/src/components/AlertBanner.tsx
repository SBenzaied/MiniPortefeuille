import type { AllocationAlert } from '../api/types'

export function AlertBanner({ alerts }: { alerts: AllocationAlert[] }) {
  if (alerts.length === 0) return null

  return (
    <div className="alert" role="alert">
      <strong>
        <span aria-hidden="true">⚠ </span>
        Répartition hors profil de risque
      </strong>
      <ul>
        {alerts.map((a) => (
          <li key={`${a.assetClass}-${a.kind}`}>{a.message}</li>
        ))}
      </ul>
    </div>
  )
}
