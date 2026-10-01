// Jeu d'icônes dessiné pour l'application : trait de 1.5, grille de 16.
const PATHS = {
  arrowLeft: 'M13 8H3.5M7.5 3.5 3 8l4.5 4.5',
  plus: 'M8 3v10M3 8h10',
  download: 'M8 2.5v8M4.5 7 8 10.5 11.5 7M3 13.5h10',
  alert: 'M8 2.2 14.3 13.3H1.7L8 2.2ZM8 6.4v3.2M8 11.4v.1',
  sortNone: 'M5 6.5 8 3.5l3 3M5 9.5l3 3 3-3',
  sortAsc: 'M4.5 9.5 8 6l3.5 3.5',
  sortDesc: 'M4.5 6.5 8 10l3.5-3.5',
  logout: 'M10 3.5h2.5v9H10M6.5 5 3.5 8l3 3M3.5 8h6.5',
} as const

export type IconName = keyof typeof PATHS

export function Icon({ name, className = 'icon' }: { name: IconName; className?: string }) {
  return (
    <svg
      className={className}
      viewBox="0 0 16 16"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d={PATHS[name]} />
    </svg>
  )
}

/** Marque de l'application : trois barres, dont une dans sa bande autorisée. */
export function BrandMark() {
  return (
    <svg className="brand-mark" viewBox="0 0 22 22" aria-hidden="true" focusable="false">
      <rect x="0.75" y="0.75" width="20.5" height="20.5" rx="4" fill="none" stroke="currentColor" strokeOpacity="0.35" strokeWidth="1.5" />
      <rect x="8" y="4" width="7" height="14" fill="#dbfc87" fillOpacity="0.22" />
      <rect x="4" y="5.5" width="13" height="2.5" rx="0.5" fill="currentColor" />
      <rect x="4" y="9.75" width="8" height="2.5" rx="0.5" fill="#dbfc87" />
      <rect x="4" y="14" width="10" height="2.5" rx="0.5" fill="currentColor" />
    </svg>
  )
}
