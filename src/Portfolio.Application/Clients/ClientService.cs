using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;
using Portfolio.Application.Common;
using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.Application.Clients;

/// <summary>
/// Consultation des clients et de leur portefeuille. Chaque requête est filtrée sur le
/// conseiller : un client d'un autre conseiller est traité comme inexistant (404), pour ne
/// pas révéler son existence.
/// </summary>
public sealed class ClientService(IPortfolioDbContext db)
{
    public async Task<IReadOnlyList<ClientSummaryDto>> ListAsync(int advisorId, RiskProfile? riskProfile, CancellationToken ct = default)
    {
        var query = db.Clients.AsNoTracking().Where(c => c.AdvisorId == advisorId);
        if (riskProfile is { } profile)
        {
            query = query.Where(c => c.RiskProfile == profile);
        }

        // Les positions sont chargées pour afficher la valeur et les alertes dans la liste.
        // Acceptable pour quelques dizaines de clients ; au-delà, il faudrait une valorisation précalculée.
        var clients = await query.Include(c => c.Positions).OrderBy(c => c.Name).ToListAsync(ct);
        var rates = await db.LoadExchangeRatesAsync(ct);

        return clients
            .Select(c =>
            {
                var valuation = PortfolioValuator.Value(c.Positions, rates);
                var alerts = AllocationAlertEvaluator.Evaluate(c.RiskProfile, valuation);
                return new ClientSummaryDto(c.Id, c.Name, c.RiskProfile, c.Positions.Count, valuation.TotalValue, alerts.Count);
            })
            .ToList();
    }

    public async Task<ClientDetailDto?> GetAsync(int advisorId, int clientId, CancellationToken ct = default)
    {
        var client = await FindOwnedClientAsync(advisorId, clientId, ct);
        if (client is null)
        {
            return null;
        }

        var positions = client.Positions
            .OrderBy(p => p.AssetClass)
            .ThenBy(p => p.Instrument)
            .Select(PositionDto.From)
            .ToList();

        return new ClientDetailDto(client.Id, client.Name, client.RiskProfile, client.Advisor!.DisplayName, positions);
    }

    public async Task<Result<ValuationDto>> GetValuationAsync(int advisorId, int clientId, string? referenceCurrency, CancellationToken ct = default)
    {
        var currency = string.IsNullOrWhiteSpace(referenceCurrency) ? Currency.Chf : referenceCurrency.Trim().ToUpperInvariant();
        var rates = await db.LoadExchangeRatesAsync(ct);
        if (!rates.IsKnown(currency))
        {
            return Result<ValuationDto>.Invalid(new Dictionary<string, string[]>
            {
                ["currency"] = [$"Devise de référence inconnue : {currency}. Devises disponibles : {string.Join(", ", rates.Codes.Order())}."],
            });
        }

        var client = await FindOwnedClientAsync(advisorId, clientId, ct);
        if (client is null)
        {
            return Result<ValuationDto>.NotFound();
        }

        var valuation = PortfolioValuator.Value(client.Positions, rates, currency);
        var alerts = AllocationAlertEvaluator.Evaluate(client.RiskProfile, valuation);
        return Result<ValuationDto>.Ok(new ValuationDto(
            valuation.ReferenceCurrency, valuation.TotalValue, valuation.Allocations, valuation.Positions, alerts));
    }

    private Task<Client?> FindOwnedClientAsync(int advisorId, int clientId, CancellationToken ct) =>
        db.Clients.AsNoTracking()
            .Include(c => c.Positions)
            .Include(c => c.Advisor)
            .SingleOrDefaultAsync(c => c.Id == clientId && c.AdvisorId == advisorId, ct);
}
