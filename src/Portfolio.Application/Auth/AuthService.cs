using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Auth;

public sealed record AuthenticatedAdvisor(int Id, string UserName, string DisplayName);

/// <summary>Vérification des identifiants d'un conseiller. L'émission du jeton JWT relève de la couche API.</summary>
public sealed class AuthService(IPortfolioDbContext db, IPasswordHasher<Advisor> hasher)
{
    // Hachage factice : on vérifie un mot de passe même si l'utilisateur n'existe pas, pour que
    // le temps de réponse ne révèle pas quels identifiants existent.
    private static readonly Lazy<string> DummyHash =
        new(() => new PasswordHasher<Advisor>().HashPassword(new Advisor { UserName = "", DisplayName = "" }, Guid.NewGuid().ToString()));

    public async Task<AuthenticatedAdvisor?> AuthenticateAsync(string? userName, string? password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var normalized = userName.Trim().ToLowerInvariant();
        var advisor = await db.Advisors.SingleOrDefaultAsync(a => a.UserName == normalized, ct);

        if (advisor is null)
        {
            hasher.VerifyHashedPassword(new Advisor { UserName = "", DisplayName = "" }, DummyHash.Value, password);
            return null;
        }

        var outcome = hasher.VerifyHashedPassword(advisor, advisor.PasswordHash, password);
        if (outcome == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (outcome == PasswordVerificationResult.SuccessRehashNeeded)
        {
            advisor.PasswordHash = hasher.HashPassword(advisor, password);
            await db.SaveChangesAsync(ct);
        }

        return new AuthenticatedAdvisor(advisor.Id, advisor.UserName, advisor.DisplayName);
    }
}
