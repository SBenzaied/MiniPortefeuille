using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Abstractions;
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
                    options.UseNpgsql(PostgresConnectionString.Normalize(connectionString));
                    break;
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException($"Fournisseur de base inconnu : {provider}.");
            }
        });
        // Le port de l'application pointe sur la même instance scoped que le DbContext concret.
        services.AddScoped<IPortfolioDbContext>(sp => sp.GetRequiredService<PortfolioDbContext>());
        services.AddScoped<IExchangeRateProvider, DbExchangeRateProvider>();
        services.AddScoped<IDatabaseSeeder, DemoDataSeeder>();
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
            await scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>().SeedAsync(ct);
        }
    }
}
