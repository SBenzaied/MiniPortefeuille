import { useCallback } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api } from '../api/portfolio'
import { RISK_PROFILES, type RiskProfile } from '../api/types'
import { Icon } from '../components/Icon'
import { Segmented } from '../components/Segmented'
import { ASSET_CLASS_LABELS, RISK_PROFILE_LABELS, formatMoney } from '../lib/format'
import { useAsync } from '../lib/useAsync'

function parseProfile(value: string | null): RiskProfile | undefined {
  return RISK_PROFILES.find((p) => p === value)
}

const PROFILE_OPTIONS = [{ value: '', label: 'Tous' }, ...RISK_PROFILES.map((p) => ({ value: p, label: RISK_PROFILE_LABELS[p] }))]

export function ClientsPage() {
  // Le filtre vit dans l'URL : il survit au rafraîchissement et au bouton « retour ».
  const [searchParams, setSearchParams] = useSearchParams()
  const riskProfile = parseProfile(searchParams.get('profil'))

  const load = useCallback(() => api.listClients(riskProfile), [riskProfile])
  const clients = useAsync(load)

  function changeProfile(value: string) {
    setSearchParams(value ? { profil: value } : {})
  }

  const list = clients.data ?? []
  const totalChf = list.reduce((sum, c) => sum + c.totalValueChf, 0)
  const outOfProfile = list.filter((c) => c.alertCount > 0).length

  return (
    <section>
      <header className="page-head">
        <div>
          <h1>Mes clients</h1>
          {clients.data && list.length > 0 && (
            <p className="page-head-meta">
              <span>{list.length === 1 ? '1 client' : `${list.length} clients`}</span>
              <span>{formatMoney(totalChf, 'CHF')} au total</span>
              {outOfProfile > 0 ? (
                <span className="status status-breach">
                  <span className="hatch-swatch" aria-hidden="true" />
                  {outOfProfile === 1 ? '1 hors profil' : `${outOfProfile} hors profil`}
                </span>
              ) : (
                <span>Tous dans leur profil</span>
              )}
            </p>
          )}
        </div>
        <Segmented legend="Profil de risque" options={PROFILE_OPTIONS} value={riskProfile ?? ''} onChange={changeProfile} />
      </header>

      {clients.status === 'error' && (
        <div className="form-error" role="alert">
          <Icon name="alert" />
          {clients.error.message}
        </div>
      )}
      {clients.status === 'loading' && !clients.data && <p className="loading">Chargement…</p>}

      {clients.data &&
        (list.length === 0 ? (
          <p className="empty">Aucun client pour ce profil.</p>
        ) : (
          <div className="table-scroll table-flush">
            <table className="data-table clients-table" aria-busy={clients.status === 'loading'}>
              <thead>
                <tr>
                  <th scope="col">Client</th>
                  <th scope="col">Profil</th>
                  <th scope="col" className="num">
                    Positions
                  </th>
                  <th scope="col" className="num">
                    Valeur (CHF)
                  </th>
                  <th scope="col">Répartition</th>
                </tr>
              </thead>
              <tbody>
                {list.map((c) => (
                  <tr key={c.id}>
                    <td className="c-name">
                      <Link to={`/clients/${c.id}`} className="client-link">
                        {c.name}
                      </Link>
                    </td>
                    <td className="c-profile">
                      <span className="profile-tag">{RISK_PROFILE_LABELS[c.riskProfile]}</span>
                    </td>
                    <td className="num c-count">{c.positionCount}</td>
                    <td className="num c-value">{formatMoney(c.totalValueChf, 'CHF')}</td>
                    <td className="c-state">
                      {c.alertCount > 0 ? (
                        <span className="status status-breach">
                          <span className="hatch-swatch" aria-hidden="true" />
                          {c.alertCount === 1 ? '1 alerte' : `${c.alertCount} alertes`}
                          <span className="status-detail">
                            {c.alertAssetClasses.map((a) => ASSET_CLASS_LABELS[a]).join(', ')}
                          </span>
                        </span>
                      ) : c.positionCount === 0 ? (
                        <span className="muted">Aucune position</span>
                      ) : (
                        <span className="status status-ok">
                          <span className="ok-swatch" aria-hidden="true" />
                          Dans le profil
                        </span>
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
