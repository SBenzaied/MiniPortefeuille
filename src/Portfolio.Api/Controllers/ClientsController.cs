using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Http;
using Portfolio.Application.Clients;
using Portfolio.Domain.Entities;

namespace Portfolio.Api.Controllers;

/// <summary>
/// Le contrôleur ne connaît que des interfaces. Le conseiller connecté n'est pas lu ici : les
/// services le reçoivent par injection (<c>ICurrentAdvisor</c>).
/// </summary>
[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class ClientsController(IClientService clients, IPortfolioExporter exporter) : ControllerBase
{
    /// <summary>Clients du conseiller connecté, filtrables par profil de risque.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<ClientSummaryDto>> List([FromQuery] RiskProfile? riskProfile, CancellationToken ct) =>
        await clients.ListAsync(riskProfile, ct);

    /// <summary>Détail d'un client et ses positions.</summary>
    [HttpGet("{clientId:int}")]
    [ProducesResponseType<ClientDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDetailDto>> Get(int clientId, CancellationToken ct)
    {
        var client = await clients.GetAsync(clientId, ct);
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
        var result = await clients.GetValuationAsync(clientId, currency, ct);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>Export CSV des positions et de leur valeur dans la devise demandée.</summary>
    [HttpGet("{clientId:int}/export.csv")]
    [Produces("text/csv")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ExportCsv(int clientId, [FromQuery] string? currency, CancellationToken ct)
    {
        var client = await clients.GetAsync(clientId, ct);
        if (client is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Ressource introuvable.");
        }

        var valuation = await clients.GetValuationAsync(clientId, currency, ct);
        return valuation.ToActionResult(this, v =>
        {
            var content = exporter.Export(client, v);
            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(content)).ToArray();
            return File(bytes, exporter.ContentType, $"portefeuille-{clientId}-{v.ReferenceCurrency}.{exporter.FileExtension}");
        });
    }
}
