using Portfolio.Application.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Clients;

/// <summary>
/// Consultation des clients du conseiller connecté et de leur portefeuille. Un client d'un autre
/// conseiller est traité comme inexistant.
/// </summary>
public interface IClientService
{
    Task<IReadOnlyList<ClientSummaryDto>> ListAsync(RiskProfile? riskProfile, CancellationToken ct = default);

    Task<ClientDetailDto?> GetAsync(int clientId, CancellationToken ct = default);

    Task<Result<ValuationDto>> GetValuationAsync(int clientId, string? referenceCurrency, CancellationToken ct = default);
}
