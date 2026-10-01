using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Http;
using Portfolio.Application.Clients;
using Portfolio.Application.Positions;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clients/{clientId:int}/positions")]
public sealed class PositionsController(IPositionService positions) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PositionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Create(int clientId, PositionRequest request, CancellationToken ct)
    {
        var result = await positions.CreateAsync(clientId, request, ct);
        return result.ToActionResult(this, created => Created($"/api/clients/{clientId}", created));
    }

    [HttpPut("{positionId:int}")]
    [ProducesResponseType<PositionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(int clientId, int positionId, PositionRequest request, CancellationToken ct)
    {
        var result = await positions.UpdateAsync(clientId, positionId, request, ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpDelete("{positionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int clientId, int positionId, CancellationToken ct)
    {
        var result = await positions.DeleteAsync(clientId, positionId, ct);
        return result.ToActionResult(this, _ => NoContent());
    }
}
