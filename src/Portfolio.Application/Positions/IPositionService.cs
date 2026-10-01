using Portfolio.Application.Clients;
using Portfolio.Application.Common;

namespace Portfolio.Application.Positions;

/// <summary>
/// Ajout, modification et suppression des positions d'un client du conseiller connecté.
/// Chaque modification laisse une entrée dans le journal d'audit.
/// </summary>
public interface IPositionService
{
    Task<Result<PositionDto>> CreateAsync(int clientId, PositionRequest? request, CancellationToken ct = default);

    Task<Result<PositionDto>> UpdateAsync(int clientId, int positionId, PositionRequest? request, CancellationToken ct = default);

    Task<Result<Unit>> DeleteAsync(int clientId, int positionId, CancellationToken ct = default);
}
