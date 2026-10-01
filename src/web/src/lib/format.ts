import type { AllocationLimit, AssetClass, RiskProfile } from '../api/types'

// Convention suisse des chiffres (apostrophe pour les milliers, point décimal), libellés en français.
// fr-CH mélange virgule et point selon qu'il s'agit d'un montant ou d'un nombre : on l'évite.
const LOCALE = 'de-CH'

export function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat(LOCALE, {
    style: 'currency',
    currency,
    currencyDisplay: 'code',
    minimumFractionDigits: 2,
  }).format(amount)
}

export function formatNumber(value: number, minDecimals = 0): string {
  return new Intl.NumberFormat(LOCALE, { minimumFractionDigits: minDecimals, maximumFractionDigits: 6 }).format(value)
}

export function formatPercent(percentage: number): string {
  // L'API renvoie des pourcentages de 0 à 100 déjà arrondis à 2 décimales.
  return `${new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 2 }).format(percentage)} %`
}

/** Bornes d'un profil pour une classe d'actifs, en texte : « 20 à 60 % », « ≤ 30 % », « ≥ 50 % ». */
export function describeLimit(limit: AllocationLimit | undefined): string {
  const min = limit?.minPercentage ?? null
  const max = limit?.maxPercentage ?? null
  const value = (n: number) => new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 2 }).format(n)
  if (min !== null && max !== null) return `${value(min)} à ${formatPercent(max)}`
  if (max !== null) return `≤ ${formatPercent(max)}`
  if (min !== null) return `≥ ${formatPercent(min)}`
  return 'Sans borne'
}

/** Écart signé en points de pourcentage : « +35.75 pts », « −30.14 pts ». */
export function formatGap(points: number): string {
  const rounded = Math.round(points * 100) / 100
  const value = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 2 }).format(Math.abs(rounded))
  return `${rounded >= 0 ? '+' : '−'}${value} pts`
}

export const RISK_PROFILE_LABELS: Record<RiskProfile, string> = {
  Prudent: 'Prudent',
  Balanced: 'Équilibré',
  Dynamic: 'Dynamique',
}

export const ASSET_CLASS_LABELS: Record<AssetClass, string> = {
  Equities: 'Actions',
  Bonds: 'Obligations',
  Cash: 'Liquidités',
  Other: 'Autres',
}
