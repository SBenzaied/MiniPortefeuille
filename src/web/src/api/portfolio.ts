import { download, request } from './http'
import type {
  ClientDetail,
  ClientSummary,
  Currency,
  LoginResponse,
  Position,
  PositionInput,
  RiskProfile,
  Valuation,
} from './types'

export const api = {
  login: (userName: string, password: string) =>
    request<LoginResponse>('/api/auth/login', { method: 'POST', body: { userName, password } }),

  listClients: (riskProfile?: RiskProfile) =>
    request<ClientSummary[]>(riskProfile ? `/api/clients?riskProfile=${riskProfile}` : '/api/clients'),

  getClient: (clientId: number) => request<ClientDetail>(`/api/clients/${clientId}`),

  getValuation: (clientId: number, currency: string) =>
    request<Valuation>(`/api/clients/${clientId}/valuation?currency=${encodeURIComponent(currency)}`),

  exportCsv: (clientId: number, currency: string) =>
    download(
      `/api/clients/${clientId}/export.csv?currency=${encodeURIComponent(currency)}`,
      `portefeuille-${clientId}-${currency}.csv`,
    ),

  listCurrencies: () => request<Currency[]>('/api/currencies'),

  createPosition: (clientId: number, input: PositionInput) =>
    request<Position>(`/api/clients/${clientId}/positions`, { method: 'POST', body: input }),

  updatePosition: (clientId: number, positionId: number, input: PositionInput) =>
    request<Position>(`/api/clients/${clientId}/positions/${positionId}`, { method: 'PUT', body: input }),

  deletePosition: (clientId: number, positionId: number) =>
    request<void>(`/api/clients/${clientId}/positions/${positionId}`, { method: 'DELETE' }),
}
