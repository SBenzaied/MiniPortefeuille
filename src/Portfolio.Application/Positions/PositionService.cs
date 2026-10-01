using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;
using Portfolio.Application.Clients;
using Portfolio.Application.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Positions;

/// <summary>
/// Ajout, modification et suppression de positions. Toute opération vérifie d'abord que le
/// client appartient au conseiller connecté, puis enregistre une entrée d'audit dans la même
/// transaction que la modification.
/// </summary>
/// <remarks>
/// L'horloge (<see cref="TimeProvider"/>) est injectée elle aussi : les tests peuvent figer la
/// date des entrées d'audit.
/// </remarks>
public sealed class PositionService(
    IPortfolioDbContext db,
    ICurrentAdvisor advisor,
    IPositionValidator validator,
    TimeProvider clock) : IPositionService
{
    public async Task<Result<PositionDto>> CreateAsync(int clientId, PositionRequest? request, CancellationToken ct = default)
    {
        if (!await OwnsClientAsync(clientId, ct))
        {
            return Result<PositionDto>.NotFound();
        }

        var (valid, errors) = await ValidateAsync(request, ct);
        if (valid is null)
        {
            return Result<PositionDto>.Invalid(errors);
        }

        var position = new Position
        {
            ClientId = clientId,
            Instrument = valid.Instrument,
            AssetClass = valid.AssetClass,
            Quantity = valid.Quantity,
            UnitPrice = valid.UnitPrice,
            CurrencyCode = valid.Currency,
        };
        // Deux SaveChanges (l'audit a besoin de l'identifiant généré), donc une transaction explicite
        // pour qu'une position ne puisse pas exister sans sa trace d'audit.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Positions.Add(position);
        await db.SaveChangesAsync(ct);

        Audit(position, AuditAction.Created, before: null, after: Snapshot(position));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Result<PositionDto>.Ok(PositionDto.From(position));
    }

    public async Task<Result<PositionDto>> UpdateAsync(int clientId, int positionId, PositionRequest? request, CancellationToken ct = default)
    {
        var position = await FindOwnedPositionAsync(clientId, positionId, ct);
        if (position is null)
        {
            return Result<PositionDto>.NotFound();
        }

        var (valid, errors) = await ValidateAsync(request, ct);
        if (valid is null)
        {
            return Result<PositionDto>.Invalid(errors);
        }

        var before = Snapshot(position);
        position.Instrument = valid.Instrument;
        position.AssetClass = valid.AssetClass;
        position.Quantity = valid.Quantity;
        position.UnitPrice = valid.UnitPrice;
        position.CurrencyCode = valid.Currency;

        Audit(position, AuditAction.Updated, before, Snapshot(position));
        await db.SaveChangesAsync(ct);

        return Result<PositionDto>.Ok(PositionDto.From(position));
    }

    public async Task<Result<Unit>> DeleteAsync(int clientId, int positionId, CancellationToken ct = default)
    {
        var position = await FindOwnedPositionAsync(clientId, positionId, ct);
        if (position is null)
        {
            return Result<Unit>.NotFound();
        }

        db.Positions.Remove(position);
        Audit(position, AuditAction.Deleted, Snapshot(position), after: null);
        await db.SaveChangesAsync(ct);

        return Result<Unit>.Ok(Unit.Value);
    }

    private async Task<(ValidPosition?, IReadOnlyDictionary<string, string[]>)> ValidateAsync(PositionRequest? request, CancellationToken ct)
    {
        var currencies = await db.Currencies.AsNoTracking().Select(c => c.Code).ToListAsync(ct);
        return validator.Validate(request, currencies);
    }

    private Task<bool> OwnsClientAsync(int clientId, CancellationToken ct)
    {
        var advisorId = advisor.Id;
        return db.Clients.AnyAsync(c => c.Id == clientId && c.AdvisorId == advisorId, ct);
    }

    private Task<Position?> FindOwnedPositionAsync(int clientId, int positionId, CancellationToken ct)
    {
        var advisorId = advisor.Id;
        return db.Positions.SingleOrDefaultAsync(
            p => p.Id == positionId && p.ClientId == clientId && p.Client!.AdvisorId == advisorId, ct);
    }

    private void Audit(Position position, AuditAction action, string? before, string? after) =>
        db.PositionAudit.Add(new PositionAuditEntry
        {
            OccurredAt = clock.GetUtcNow(),
            AdvisorId = advisor.Id,
            ClientId = position.ClientId,
            PositionId = position.Id,
            Action = action,
            Before = before,
            After = after,
        });

    private static string Snapshot(Position p) => string.Create(CultureInfo.InvariantCulture,
        $"{p.Instrument} | {p.AssetClass} | {p.Quantity.Normalize()} x {p.UnitPrice.Normalize()} {p.CurrencyCode}");
}
