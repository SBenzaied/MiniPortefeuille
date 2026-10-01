// Contrats JSON de l'API (miroir des DTO .NET).

export const RISK_PROFILES = ['Prudent', 'Balanced', 'Dynamic'] as const
export type RiskProfile = (typeof RISK_PROFILES)[number]

export const ASSET_CLASSES = ['Equities', 'Bonds', 'Cash', 'Other'] as const
export type AssetClass = (typeof ASSET_CLASSES)[number]

export interface Advisor {
  id: number
  userName: string
  displayName: string
}

export interface LoginResponse {
  accessToken: string
  expiresAt: string
  advisor: Advisor
}

export interface ClientSummary {
  id: number
  name: string
  riskProfile: RiskProfile
  positionCount: number
  totalValueChf: number
  alertCount: number
  /** Classes d'actifs hors profil. */
  alertAssetClasses: AssetClass[]
}

export interface Position {
  id: number
  instrument: string
  assetClass: AssetClass
  quantity: number
  unitPrice: number
  currency: string
  marketValue: number
}

export interface ClientDetail {
  id: number
  name: string
  riskProfile: RiskProfile
  advisorName: string
  positions: Position[]
}

export interface Allocation {
  assetClass: AssetClass
  amount: number
  percentage: number
}

export interface AllocationAlert {
  assetClass: AssetClass
  kind: 'AboveMaximum' | 'BelowMinimum'
  actualPercentage: number
  limitPercentage: number
  message: string
}

/** Borne du profil de risque pour une classe d'actifs, en pourcent (bornes incluses). */
export interface AllocationLimit {
  assetClass: AssetClass
  minPercentage: number | null
  maxPercentage: number | null
}

export interface Valuation {
  referenceCurrency: string
  totalValue: number
  allocations: Allocation[]
  positions: { positionId: number; valueInReferenceCurrency: number }[]
  alerts: AllocationAlert[]
  limits: AllocationLimit[]
}

export interface Currency {
  code: string
  name: string
  rateToChf: number
}

/** Corps envoyé pour créer ou modifier une position. */
export interface PositionInput {
  instrument: string
  assetClass: AssetClass | ''
  quantity: number | null
  unitPrice: number | null
  currency: string
}
