import { useCallback, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../api/http'
import { api } from '../api/portfolio'
import type { Position, PositionInput } from '../api/types'
import { AlertBanner } from '../components/AlertBanner'
import { AllocationChart } from '../components/AllocationChart'
import { PositionForm } from '../components/PositionForm'
import { PositionsTable, type PositionRow } from '../components/PositionsTable'
import { RISK_PROFILE_LABELS, formatMoney } from '../lib/format'
import { useAsync } from '../lib/useAsync'

type Editing = { mode: 'create' } | { mode: 'edit'; position: Position } | null

export function ClientPage() {
  const clientId = Number(useParams().clientId)
  const [currency, setCurrency] = useState('CHF')
  const [editing, setEditing] = useState<Editing>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const loadClient = useCallback(() => api.getClient(clientId), [clientId])
  const loadValuation = useCallback(() => api.getValuation(clientId, currency), [clientId, currency])
  const loadCurrencies = useCallback(() => api.listCurrencies(), [])
  const client = useAsync(loadClient)
  const valuation = useAsync(loadValuation)
  const currencies = useAsync(loadCurrencies)

  const rows = useMemo<PositionRow[]>(() => {
    if (!client.data) return []
    const values = new Map(valuation.data?.positions.map((p) => [p.positionId, p.valueInReferenceCurrency]))
    return client.data.positions.map((p) => ({ ...p, referenceValue: values.get(p.id) ?? 0 }))
  }, [client.data, valuation.data])

  const refresh = () => {
    client.reload()
    valuation.reload()
  }

  async function save(input: PositionInput) {
    if (editing?.mode === 'edit') {
      await api.updatePosition(clientId, editing.position.id, input)
    } else {
      await api.createPosition(clientId, input)
    }
    setEditing(null)
    refresh()
  }

  async function exportCsv() {
    setActionError(null)
    try {
      await api.exportCsv(clientId, currency)
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : "L'export a échoué.")
    }
  }

  async function remove(position: Position) {
    if (!window.confirm(`Supprimer la position « ${position.instrument} » ?`)) return
    setActionError(null)
    try {
      await api.deletePosition(clientId, position.id)
      refresh()
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : 'La suppression a échoué.')
    }
  }

  if (client.status === 'error' && !client.data) {
    return (
      <section>
        <p className="form-error" role="alert">
          {client.error.status === 404 ? 'Client introuvable.' : client.error.message}
        </p>
        <Link to="/clients">← Retour à la liste</Link>
      </section>
    )
  }
  if (!client.data) return <p className="muted">Chargement…</p>

  const detail = client.data
  const val = valuation.data

  return (
    <section>
      <Link to="/clients" className="back-link">
        ← Mes clients
      </Link>
      <div className="page-header">
        <div>
          <h1>{detail.name}</h1>
          <p className="muted">
            Profil <span className={`badge profile-${detail.riskProfile.toLowerCase()}`}>{RISK_PROFILE_LABELS[detail.riskProfile]}</span>
            {' · '}Conseiller : {detail.advisorName}
          </p>
        </div>
        <div className="field inline">
          <label htmlFor="reference-currency">Devise de référence</label>
          <select id="reference-currency" value={currency} onChange={(e) => setCurrency(e.target.value)}>
            {(currencies.data ?? [{ code: 'CHF', name: 'Franc suisse', rateToChf: 1 }]).map((c) => (
              <option key={c.code} value={c.code}>
                {c.code}
              </option>
            ))}
          </select>
        </div>
      </div>

      {valuation.status === 'error' && (
        <div className="form-error" role="alert">
          {valuation.error.message}
        </div>
      )}

      {val && (
        <>
          <AlertBanner alerts={val.alerts} />
          <div className="summary-grid">
            <div className="card total-card">
              <h2 className="card-title">Valeur totale</h2>
              <p className="total-value">{formatMoney(val.totalValue, val.referenceCurrency)}</p>
              <p className="muted small">Taux de change fixes (données fictives)</p>
            </div>
            <div className="card">
              <h2 className="card-title">Répartition par classe d'actifs</h2>
              <AllocationChart allocations={val.allocations} currency={val.referenceCurrency} />
            </div>
          </div>
        </>
      )}

      <div className="section-header">
        <h2>Positions</h2>
        <div className="button-row">
          <button type="button" className="secondary" onClick={exportCsv} disabled={detail.positions.length === 0}>
            Exporter en CSV
          </button>
          {!editing && (
            <button type="button" onClick={() => setEditing({ mode: 'create' })}>
              Ajouter une position
            </button>
          )}
        </div>
      </div>

      {actionError && (
        <div className="form-error" role="alert">
          {actionError}
        </div>
      )}

      {editing && (
        <div className="card">
          <PositionForm
            key={editing.mode === 'edit' ? editing.position.id : 'new'}
            position={editing.mode === 'edit' ? editing.position : undefined}
            currencies={currencies.data ?? []}
            onSubmit={save}
            onCancel={() => setEditing(null)}
          />
        </div>
      )}

      <PositionsTable
        rows={rows}
        referenceCurrency={val?.referenceCurrency ?? currency}
        onEdit={(position) => setEditing({ mode: 'edit', position })}
        onDelete={remove}
      />
    </section>
  )
}
