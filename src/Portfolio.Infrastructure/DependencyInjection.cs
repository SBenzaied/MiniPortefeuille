using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Abstractions;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// <c>Database:Provider</c> vaut <c>Postgres</c> (par défaut, utilisé par docker compose) ou
    /// <c>Sqlite</c> (lancement local sans Docker et tests d'intégration).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("Portfolio")
            ?? throw new InvalidOperationException("La chaîne de connexion 'ConnectionStrings:Portfolio' n'est pas configurée.");

        services.AddDbContext<PortfolioDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "postgres":
                    options.UseNpgsql(connectionString);
                    break;
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException($"Fournisseur de base inconnu : {provider}.");
            }
        });
        services.AddScoped<IPortfolioDbContext>(sp => sp.GetRequiredService<PortfolioDbContext>());
        return services;
    }

    /// <summary>
    /// Applique les migrations (PostgreSQL) ou crée le schéma (SQLite), puis charge le jeu
    /// d'essai si <c>Database:SeedDemoData</c> est vrai et que la base est vide.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

        if (db.Database.IsNpgsql())
        {
            await db.Database.MigrateAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }

        if (configuration.GetValue("Database:SeedDemoData", defaultValue: false))
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Advisor>>();
            await DemoDataSeeder.SeedAsync(db, hasher, ct);
        }
    }
}
