import type { AllocationAlert } from '../api/types'
import { Icon } from './Icon'

export function AlertBanner({ alerts }: { alerts: AllocationAlert[] }) {
  if (alerts.length === 0) return null

  return (
    <div className="breach-notes" role="alert">
      <p className="breach-notes-title">
        <Icon name="alert" />
        Répartition hors profil de risque
      </p>
      <ul>
        {alerts.map((a) => (
          <li key={`${a.assetClass}-${a.kind}`}>{a.message}</li>
        ))}
      </ul>
    </div>
  )
}
