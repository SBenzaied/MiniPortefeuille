using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence;

/// <summary>
/// Jeu d'essai fictif chargé au démarrage si la base est vide. Les noms de clients, les
/// quantités, les prix et les taux de change sont inventés.
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>Mot de passe des comptes de démonstration (documenté dans le README, données fictives).</summary>
    public const string DemoPassword = "Demo-2026!";

    public static async Task SeedAsync(PortfolioDbContext db, IPasswordHasher<Advisor> hasher, CancellationToken ct = default)
    {
        if (await db.Currencies.AnyAsync(ct))
        {
            return;
        }

        db.Currencies.AddRange(
            new Currency { Code = "CHF", Name = "Franc suisse", RateToChf = 1m },
            new Currency { Code = "EUR", Name = "Euro", RateToChf = 0.94m },
            new Currency { Code = "USD", Name = "Dollar américain", RateToChf = 0.80m });

        var alice = Advisor("amartin", "Alice Martin", hasher);
        var bruno = Advisor("bnguyen", "Bruno Nguyen", hasher);

        alice.Clients =
        [
            // Prudent, bien aligné : majorité d'obligations.
            Client("Famille Dubois", RiskProfile.Prudent,
                P("Confédération 0.5% 2031", AssetClass.Bonds, 400, 98.40m, "CHF"),
                P("Bund 2.2% 2034", AssetClass.Bonds, 250, 97.10m, "EUR"),
                P("Nestlé SA", AssetClass.Equities, 60, 88.50m, "CHF"),
                P("Compte courant CHF", AssetClass.Cash, 1, 25_000m, "CHF")),

            // Prudent mais trop exposé aux actions : déclenche une alerte.
            Client("Jean Rochat", RiskProfile.Prudent,
                P("Novartis AG", AssetClass.Equities, 300, 92.30m, "CHF"),
                P("Apple Inc.", AssetClass.Equities, 120, 215.00m, "USD"),
                P("Confédération 1.5% 2029", AssetClass.Bonds, 150, 101.20m, "CHF"),
                P("Compte courant CHF", AssetClass.Cash, 1, 10_000m, "CHF")),

            Client("Sophie Keller", RiskProfile.Balanced,
                P("iShares Core MSCI World", AssetClass.Equities, 500, 98.70m, "USD"),
                P("Roche Holding", AssetClass.Equities, 40, 265.00m, "CHF"),
                P("US Treasury 4% 2030", AssetClass.Bonds, 300, 99.20m, "USD"),
                P("OAT 3% 2033", AssetClass.Bonds, 200, 100.80m, "EUR"),
                P("Fonds immobilier suisse", AssetClass.Other, 100, 145.00m, "CHF"),
                P("Compte courant EUR", AssetClass.Cash, 1, 12_000m, "EUR")),

            // Dynamique mais beaucoup de liquidités : déclenche deux alertes.
            Client("Marc Weber", RiskProfile.Dynamic,
                P("Compte courant CHF", AssetClass.Cash, 1, 180_000m, "CHF"),
                P("UBS Group AG", AssetClass.Equities, 1_000, 27.40m, "CHF"),
                P("Microsoft Corp.", AssetClass.Equities, 50, 430.00m, "USD")),
        ];

        bruno.Clients =
        [
            Client("Fondation Lémanique", RiskProfile.Balanced,
                P("SPDR S&P 500 ETF", AssetClass.Equities, 150, 560.00m, "USD"),
                P("Confédération 0.5% 2031", AssetClass.Bonds, 900, 98.40m, "CHF"),
                P("Or physique (once)", AssetClass.Other, 20, 2_100.00m, "USD"),
                P("Compte courant CHF", AssetClass.Cash, 1, 40_000m, "CHF")),

            Client("Claire Favre", RiskProfile.Dynamic,
                P("ASML Holding", AssetClass.Equities, 60, 690.00m, "EUR"),
                P("Logitech International", AssetClass.Equities, 200, 78.00m, "CHF"),
                P("Nvidia Corp.", AssetClass.Equities, 150, 120.00m, "USD"),
                P("Compte courant USD", AssetClass.Cash, 1, 8_000m, "USD")),

            // Client sans position : vérifie l'affichage d'un portefeuille vide.
            Client("Paul Meier", RiskProfile.Prudent),
        ];

        db.Advisors.AddRange(alice, bruno);
        await db.SaveChangesAsync(ct);
    }

    private static Advisor Advisor(string userName, string displayName, IPasswordHasher<Advisor> hasher)
    {
        var advisor = new Advisor { UserName = userName, DisplayName = displayName };
        advisor.PasswordHash = hasher.HashPassword(advisor, DemoPassword);
        return advisor;
    }

    private static Client Client(string name, RiskProfile profile, params Position[] positions) =>
        new() { Name = name, RiskProfile = profile, Positions = [.. positions] };

    private static Position P(string instrument, AssetClass assetClass, decimal quantity, decimal unitPrice, string currency) =>
        new() { Instrument = instrument, AssetClass = assetClass, Quantity = quantity, UnitPrice = unitPrice, CurrencyCode = currency };
}
