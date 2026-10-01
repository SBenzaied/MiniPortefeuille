import type { AssetClass, RiskProfile } from '../api/types'

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

export function formatNumber(value: number, maxDecimals = 6): string {
  return new Intl.NumberFormat(LOCALE, { maximumFractionDigits: maxDecimals }).format(value)
}

export function formatPercent(percentage: number): string {
  // L'API renvoie des pourcentages de 0 à 100 déjà arrondis à 2 décimales.
  return `${new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 2 }).format(percentage)} %`
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
