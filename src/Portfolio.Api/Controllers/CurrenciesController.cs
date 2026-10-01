using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Currencies;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/currencies")]
public sealed class CurrenciesController(ICurrencyService currencies) : ControllerBase
{
    /// <summary>Devises connues et leur taux fixe vers le CHF (alimente les listes du front).</summary>
    [HttpGet]
    public Task<IReadOnlyList<CurrencyDto>> List(CancellationToken ct) => currencies.ListAsync(ct);
}
