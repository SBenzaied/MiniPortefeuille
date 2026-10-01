import { useCallback } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api } from '../api/portfolio'
import { RISK_PROFILES, type RiskProfile } from '../api/types'
import { RISK_PROFILE_LABELS, formatMoney } from '../lib/format'
import { useAsync } from '../lib/useAsync'

function parseProfile(value: string | null): RiskProfile | undefined {
  return RISK_PROFILES.find((p) => p === value)
}

export function ClientsPage() {
  // Le filtre vit dans l'URL : il survit au rafraîchissement et au bouton « retour ».
  const [searchParams, setSearchParams] = useSearchParams()
  const riskProfile = parseProfile(searchParams.get('profil'))

  const load = useCallback(() => api.listClients(riskProfile), [riskProfile])
  const clients = useAsync(load)

  function changeProfile(value: string) {
    setSearchParams(value ? { profil: value } : {})
  }

  return (
    <section>
      <div className="page-header">
        <h1>Mes clients</h1>
        <div className="field inline">
          <label htmlFor="profile-filter">Profil de risque</label>
          <select id="profile-filter" value={riskProfile ?? ''} onChange={(e) => changeProfile(e.target.value)}>
            <option value="">Tous</option>
            {RISK_PROFILES.map((p) => (
              <option key={p} value={p}>
                {RISK_PROFILE_LABELS[p]}
              </option>
            ))}
          </select>
        </div>
      </div>

      {clients.status === 'error' && (
        <div className="form-error" role="alert">
          {clients.error.message}
        </div>
      )}
      {clients.status === 'loading' && !clients.data && <p className="muted">Chargement…</p>}

      {clients.data &&
        (clients.data.length === 0 ? (
          <p className="muted">Aucun client pour ce profil.</p>
        ) : (
          <div className="table-scroll">
            <table className="data-table" aria-busy={clients.status === 'loading'}>
              <thead>
                <tr>
                  <th scope="col">Client</th>
                  <th scope="col">Profil</th>
                  <th scope="col" className="num">Positions</th>
                  <th scope="col" className="num">Valeur (CHF)</th>
                  <th scope="col">Alerte</th>
                </tr>
              </thead>
              <tbody>
                {clients.data.map((c) => (
                  <tr key={c.id}>
                    <td>
                      <Link to={`/clients/${c.id}`}>{c.name}</Link>
                    </td>
                    <td>
                      <span className={`badge profile-${c.riskProfile.toLowerCase()}`}>{RISK_PROFILE_LABELS[c.riskProfile]}</span>
                    </td>
                    <td className="num">{c.positionCount}</td>
                    <td className="num">{formatMoney(c.totalValueChf, 'CHF')}</td>
                    <td>
                      {c.alertCount > 0 ? (
                        <span className="badge warning">
                          <span aria-hidden="true">⚠ </span>
                          {c.alertCount === 1 ? '1 alerte' : `${c.alertCount} alertes`}
                        </span>
                      ) : (
                        <span className="muted">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </section>
  )
}
