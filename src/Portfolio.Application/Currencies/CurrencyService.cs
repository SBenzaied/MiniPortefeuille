using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;

namespace Portfolio.Application.Currencies;

public sealed record CurrencyDto(string Code, string Name, decimal RateToChf);

/// <summary>Devises connues et leur taux fixe vers le CHF.</summary>
public interface ICurrencyService
{
    Task<IReadOnlyList<CurrencyDto>> ListAsync(CancellationToken ct = default);
}

public sealed class CurrencyService(IPortfolioDbContext db) : ICurrencyService
{
    public async Task<IReadOnlyList<CurrencyDto>> ListAsync(CancellationToken ct = default) =>
        await db.Currencies.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Code, c.Name, c.RateToChf))
            .ToListAsync(ct);
}
