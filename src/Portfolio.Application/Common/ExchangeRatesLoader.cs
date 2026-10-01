using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;
using Portfolio.Domain.Valuation;

namespace Portfolio.Application.Common;

internal static class ExchangeRatesLoader
{
    public static async Task<ExchangeRates> LoadExchangeRatesAsync(this IPortfolioDbContext db, CancellationToken ct) =>
        ExchangeRates.From(await db.Currencies.AsNoTracking().ToListAsync(ct));
}
