using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Abstractions;

/// <summary>
/// Port d'accès aux données utilisé par la couche application. L'implémentation EF Core
/// (PostgreSQL ou SQLite) vit dans Portfolio.Infrastructure.
/// </summary>
public interface IPortfolioDbContext
{
    DbSet<Advisor> Advisors { get; }
    DbSet<Client> Clients { get; }
    DbSet<Position> Positions { get; }
    DbSet<Currency> Currencies { get; }
    DbSet<PositionAuditEntry> PositionAudit { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
