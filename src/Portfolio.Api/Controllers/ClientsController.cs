using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Auth;
using Portfolio.Api.Http;
using Portfolio.Application.Clients;
using Portfolio.Domain.Entities;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class ClientsController(ClientService clients) : ControllerBase
{
    /// <summary>Clients du conseiller connecté, filtrables par profil de risque.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<ClientSummaryDto>> List([FromQuery] RiskProfile? riskProfile, CancellationToken ct) =>
        await clients.ListAsync(User.GetAdvisorId(), riskProfile, ct);

    /// <summary>Détail d'un client et ses positions.</summary>
    [HttpGet("{clientId:int}")]
    [ProducesResponseType<ClientDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDetailDto>> Get(int clientId, CancellationToken ct)
    {
        var client = await clients.GetAsync(User.GetAdvisorId(), clientId, ct);
        return client is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Ressource introuvable.")
            : client;
    }

    /// <summary>Valeur totale, répartition par classe d'actifs et alertes, dans la devise demandée (CHF par défaut).</summary>
    [HttpGet("{clientId:int}/valuation")]
    [ProducesResponseType<ValuationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetValuation(int clientId, [FromQuery] string? currency, CancellationToken ct)
    {
        var result = await clients.GetValuationAsync(User.GetAdvisorId(), clientId, currency, ct);
        return result.ToActionResult(this, Ok);
    }
}
