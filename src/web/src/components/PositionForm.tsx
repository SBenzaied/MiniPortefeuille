import { useId, useState, type FormEvent } from 'react'
import { ApiError } from '../api/http'
import { ASSET_CLASSES, type Currency, type Position, type PositionInput } from '../api/types'
import { ASSET_CLASS_LABELS } from '../lib/format'
import { Icon } from './Icon'

interface Props {
  /** Position à modifier ; absente pour une création. */
  position?: Position
  currencies: Currency[]
  /** Doit rejeter une ApiError pour que les erreurs de validation s'affichent. */
  onSubmit: (input: PositionInput) => Promise<void>
  onCancel: () => void
}

type Field = 'instrument' | 'assetClass' | 'quantity' | 'unitPrice' | 'currency'

/** Champ vide → null (l'API signalera le champ obligatoire) ; texte non numérique → NaN. */
function toNumber(value: string): number | null {
  if (value.trim() === '') return null
  return Number(value.trim().replace(/[\s']/g, '').replace(',', '.'))
}

/**
 * Formulaire d'ajout/modification. La validation fait autorité côté API : le formulaire
 * n'empêche pas l'envoi et affiche sous chaque champ les erreurs renvoyées par le serveur.
 */
export function PositionForm({ position, currencies, onSubmit, onCancel }: Props) {
  const id = useId()
  const [values, setValues] = useState({
    instrument: position?.instrument ?? '',
    assetClass: position?.assetClass ?? '',
    quantity: position ? String(position.quantity) : '',
    unitPrice: position ? String(position.unitPrice) : '',
    currency: position?.currency ?? 'CHF',
  })
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  function update(field: Field, value: string) {
    setValues((v) => ({ ...v, [field]: value }))
    // L'erreur d'un champ disparaît dès qu'on le corrige.
    setFieldErrors(({ [field]: _removed, ...rest }) => rest)
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)

    const quantity = toNumber(values.quantity)
    const unitPrice = toNumber(values.unitPrice)
    // Seul contrôle local : un texte non numérique ne peut pas être transmis en JSON comme nombre.
    const formatErrors: Record<string, string[]> = {}
    if (Number.isNaN(quantity)) formatErrors.quantity = ['Saisissez un nombre (ex. 12.5).']
    if (Number.isNaN(unitPrice)) formatErrors.unitPrice = ['Saisissez un nombre (ex. 98.40).']
    if (Object.keys(formatErrors).length > 0) {
      setFieldErrors(formatErrors)
      return
    }

    setSubmitting(true)
    try {
      await onSubmit({
        instrument: values.instrument,
        assetClass: values.assetClass as PositionInput['assetClass'],
        quantity,
        unitPrice,
        currency: values.currency,
      })
    } catch (error) {
      if (error instanceof ApiError) {
        const { '': general, ...byField } = error.fieldErrors
        setFieldErrors(byField)
        setFormError(general?.join(' ') ?? error.message)
      } else {
        setFormError('Une erreur inattendue est survenue.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  function errorProps(field: Field) {
    const messages = fieldErrors[field]
    return messages?.length
      ? { 'aria-invalid': true as const, 'aria-describedby': `${id}-${field}-error` }
      : {}
  }

  function errorText(field: Field) {
    const messages = fieldErrors[field]
    return messages?.length ? (
      <p id={`${id}-${field}-error`} className="field-error">
        {messages.join(' ')}
      </p>
    ) : null
  }

  return (
    <form className="position-form" onSubmit={handleSubmit} noValidate aria-labelledby={`${id}-title`}>
      <h3 id={`${id}-title`}>{position ? 'Modifier la position' : 'Nouvelle position'}</h3>

      {formError && (
        <div className="form-error" role="alert">
          <Icon name="alert" />
          {formError}
        </div>
      )}

      <div className="form-grid">
        <div className="field field-wide">
          <label htmlFor={`${id}-instrument`}>Instrument</label>
          <input
            id={`${id}-instrument`}
            value={values.instrument}
            onChange={(e) => update('instrument', e.target.value)}
            autoComplete="off"
            {...errorProps('instrument')}
          />
          {errorText('instrument')}
        </div>

        <div className="field">
          <label htmlFor={`${id}-assetClass`}>Classe d'actifs</label>
          <select
            id={`${id}-assetClass`}
            value={values.assetClass}
            onChange={(e) => update('assetClass', e.target.value)}
            {...errorProps('assetClass')}
          >
            <option value="">— Choisir —</option>
            {ASSET_CLASSES.map((c) => (
              <option key={c} value={c}>
                {ASSET_CLASS_LABELS[c]}
              </option>
            ))}
          </select>
          {errorText('assetClass')}
        </div>

        <div className="field">
          <label htmlFor={`${id}-quantity`}>Quantité</label>
          <input
            id={`${id}-quantity`}
            inputMode="decimal"
            value={values.quantity}
            onChange={(e) => update('quantity', e.target.value)}
            {...errorProps('quantity')}
          />
          {errorText('quantity')}
        </div>

        <div className="field">
          <label htmlFor={`${id}-unitPrice`}>Prix unitaire</label>
          <input
            id={`${id}-unitPrice`}
            inputMode="decimal"
            value={values.unitPrice}
            onChange={(e) => update('unitPrice', e.target.value)}
            {...errorProps('unitPrice')}
          />
          {errorText('unitPrice')}
        </div>

        <div className="field">
          <label htmlFor={`${id}-currency`}>Devise</label>
          <select
            id={`${id}-currency`}
            value={values.currency}
            onChange={(e) => update('currency', e.target.value)}
            {...errorProps('currency')}
          >
            {currencies.map((c) => (
              <option key={c.code} value={c.code}>
                {c.code} – {c.name}
              </option>
            ))}
          </select>
          {errorText('currency')}
        </div>
      </div>

      <div className="form-actions">
        <button type="button" className="button secondary" onClick={onCancel} disabled={submitting}>
          Annuler
        </button>
        <button type="submit" className="button" disabled={submitting}>
          {submitting ? 'Enregistrement…' : 'Enregistrer'}
        </button>
      </div>
    </form>
  )
}
