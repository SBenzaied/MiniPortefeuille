import { useId } from 'react'

interface Option {
  value: string
  label: string
}

interface Props {
  legend: string
  /** Libellé affiché à côté du groupe ; le legend reste lu par les lecteurs d'écran. */
  showLabel?: boolean
  options: Option[]
  value: string
  onChange: (value: string) => void
}

/** Groupe de boutons radio présenté comme un sélecteur segmenté (navigation aux flèches native). */
export function Segmented({ legend, showLabel = true, options, value, onChange }: Props) {
  const name = useId()
  return (
    <div className="filter">
      {showLabel && (
        <span className="filter-label" aria-hidden="true">
          {legend}
        </span>
      )}
      <fieldset className="segmented">
        <legend>{legend}</legend>
        {options.map((o) => (
          <label key={o.value}>
            <input
              type="radio"
              name={name}
              value={o.value}
              checked={value === o.value}
              onChange={() => onChange(o.value)}
            />
            {o.label}
          </label>
        ))}
      </fieldset>
    </div>
  )
}
