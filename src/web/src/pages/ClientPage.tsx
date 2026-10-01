import { useCallback, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../api/http'
import { api } from '../api/portfolio'
import type { Position, PositionInput } from '../api/types'
import { AlertBanner } from '../components/AlertBanner'
import { AllocationBands } from '../components/AllocationBands'
import { Icon } from '../components/Icon'
import { PositionForm } from '../components/PositionForm'
import { PositionsTable, type PositionRow } from '../components/PositionsTable'
import { Segmented } from '../components/Segmented'
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

  // La confirmation a lieu dans la ligne du tableau (PositionsTable).
  async function remove(position: Position) {
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
          <Icon name="alert" />
          {client.error.status === 404 ? 'Client introuvable.' : client.error.message}
        </p>
        <Link to="/clients" className="back-link">
          <Icon name="arrowLeft" />
          Retour à la liste
        </Link>
      </section>
    )
  }
  if (!client.data) return <p className="loading">Chargement…</p>

  const detail = client.data
  const val = valuation.data
  const currencyOptions = (currencies.data ?? [{ code: 'CHF', name: 'Franc suisse', rateToChf: 1 }]).map((c) => ({
    value: c.code,
    label: c.code,
  }))
  const inBreach = (val?.alerts.length ?? 0) > 0

  return (
    <section>
      <Link to="/clients" className="back-link">
        <Icon name="arrowLeft" />
        Mes clients
      </Link>

      <header className="page-head">
        <div>
          <h1>{detail.name}</h1>
          <p className="page-head-meta">
            <span>
              Profil <span className="profile-tag">{RISK_PROFILE_LABELS[detail.riskProfile]}</span>
            </span>
            <span>Conseiller : {detail.advisorName}</span>
          </p>
        </div>
        <div className="sheet-total">
          <span className="sheet-total-label">Valeur totale</span>
          <strong className="sheet-total-value">{val ? formatMoney(val.totalValue, val.referenceCurrency) : '—'}</strong>
          <Segmented legend="Devise de référence" options={currencyOptions} value={currency} onChange={setCurrency} />
        </div>
      </header>

      {valuation.status === 'error' && (
        <div className="form-error" role="alert">
          <Icon name="alert" />
          {valuation.error.message}
        </div>
      )}

      {val && (
        <section className="committee" aria-labelledby="committee-title">
          <div className="section-head">
            <div className="section-head-title">
              <h2 id="committee-title">Répartition face au profil {RISK_PROFILE_LABELS[detail.riskProfile].toLowerCase()}</h2>
              {val.totalValue > 0 &&
                (inBreach ? (
                  <span className="status status-breach">
                    <span className="hatch-swatch" aria-hidden="true" />
                    Hors profil
                  </span>
                ) : (
                  <span className="status status-ok">
                    <span className="ok-swatch" aria-hidden="true" />
                    Dans le profil
                  </span>
                ))}
            </div>
          </div>
          <AllocationBands allocations={val.allocations} limits={val.limits} alerts={val.alerts} currency={val.referenceCurrency} />
          <AlertBanner alerts={val.alerts} />
          <div className="committee-foot small muted">
            <div className="committee-legend" aria-hidden="true">
              <span>
                <span className="ok-swatch" />
                Plage autorisée par le profil
              </span>
              <span>
                <span className="hatch-swatch" />
                Écart à la borne
              </span>
            </div>
            <p>Taux de change fixes (données fictives)</p>
          </div>
        </section>
      )}

      <div className="section-head">
        <h2>Positions</h2>
        <div className="button-row">
          <button type="button" className="button secondary" onClick={exportCsv} disabled={detail.positions.length === 0}>
            <Icon name="download" />
            Exporter en CSV
          </button>
          {!editing && (
            <button type="button" className="button" onClick={() => setEditing({ mode: 'create' })}>
              <Icon name="plus" />
              Ajouter une position
            </button>
          )}
        </div>
      </div>

      {actionError && (
        <div className="form-error" role="alert">
          <Icon name="alert" />
          {actionError}
        </div>
      )}

      {editing && (
        <div className="position-panel">
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
        total={val?.totalValue}
        onEdit={(position) => setEditing({ mode: 'edit', position })}
        onDelete={remove}
      />
    </section>
  )
}
