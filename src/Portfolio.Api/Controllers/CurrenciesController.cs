using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;

namespace Portfolio.Api.Controllers;

public sealed record CurrencyDto(string Code, string Name, decimal RateToChf);

[ApiController]
[Authorize]
[Route("api/currencies")]
public sealed class CurrenciesController(IPortfolioDbContext db) : ControllerBase
{
    /// <summary>Devises connues et leur taux fixe vers le CHF (alimente les listes du front).</summary>
    [HttpGet]
    public async Task<IReadOnlyList<CurrencyDto>> List(CancellationToken ct) =>
        await db.Currencies.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Code, c.Name, c.RateToChf))
            .ToListAsync(ct);
}
