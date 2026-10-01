using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.UnitTests;

/// <summary>
/// Base SQLite en mémoire, propre à chaque test, avec un jeu de données minimal. Les requêtes
/// EF Core (Include, transactions) ne se simulent pas proprement avec Moq : on garde un vrai
/// DbContext et on simule toutes les autres dépendances des services.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public const int AliceId = 1;
    public const int BrunoId = 2;

    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Context = new PortfolioDbContext(new DbContextOptionsBuilder<PortfolioDbContext>().UseSqlite(_connection).Options);
        Context.Database.EnsureCreated();

        Context.Currencies.AddRange(
            new Currency { Code = "CHF", Name = "Franc suisse", RateToChf = 1m },
            new Currency { Code = "USD", Name = "Dollar américain", RateToChf = 0.80m });
        Context.Advisors.AddRange(
            new Advisor { Id = AliceId, UserName = "amartin", DisplayName = "Alice Martin", PasswordHash = "hash-alice" },
            new Advisor { Id = BrunoId, UserName = "bnguyen", DisplayName = "Bruno Nguyen" });
        Rochat = new Client
        {
            Name = "Jean Rochat",
            RiskProfile = RiskProfile.Prudent,
            AdvisorId = AliceId,
            Positions =
            [
                new Position { Instrument = "Novartis AG", AssetClass = AssetClass.Equities, Quantity = 300, UnitPrice = 92.30m, CurrencyCode = "CHF" },
                new Position { Instrument = "Apple Inc.", AssetClass = AssetClass.Equities, Quantity = 120, UnitPrice = 215m, CurrencyCode = "USD" },
            ],
        };
        Context.Clients.Add(Rochat);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public PortfolioDbContext Context { get; }

    /// <summary>Client d'Alice, avec deux positions.</summary>
    public Client Rochat { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
