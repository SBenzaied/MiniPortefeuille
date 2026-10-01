using Portfolio.Domain.Valuation;

namespace Portfolio.Application.Abstractions;

/// <summary>
/// Source des taux de change. L'implémentation actuelle lit les taux fixes en base
/// (Portfolio.Infrastructure) ; un fournisseur de données de marché la remplacerait ici.
/// </summary>
public interface IExchangeRateProvider
{
    Task<ExchangeRates> GetRatesAsync(CancellationToken ct = default);
}
