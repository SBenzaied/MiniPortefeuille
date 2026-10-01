using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;
using Portfolio.Domain.Valuation;

namespace Portfolio.Infrastructure.Persistence;

/// <summary>Taux de change fixes lus dans la table des devises.</summary>
public sealed class DbExchangeRateProvider(PortfolioDbContext db) : IExchangeRateProvider
{
    public async Task<ExchangeRates> GetRatesAsync(CancellationToken ct = default) =>
        ExchangeRates.From(await db.Currencies.AsNoTracking().ToListAsync(ct));
}
