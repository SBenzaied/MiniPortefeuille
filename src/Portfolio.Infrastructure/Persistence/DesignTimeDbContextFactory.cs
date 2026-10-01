using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Portfolio.Infrastructure.Persistence;

/// <summary>
/// Utilisé uniquement par <c>dotnet ef migrations add</c>, qui n'ouvre pas de connexion :
/// la chaîne ci-dessous ne contient donc aucun identifiant réel.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseNpgsql("Host=localhost;Database=portfolio")
            .Options);
}
