namespace Portfolio.Application.Auth;

public sealed record AuthenticatedAdvisor(int Id, string UserName, string DisplayName);

/// <summary>Vérification des identifiants d'un conseiller. L'émission du jeton relève de la couche API.</summary>
public interface IAuthService
{
    /// <returns>Le conseiller, ou <c>null</c> si l'identifiant ou le mot de passe est incorrect.</returns>
    Task<AuthenticatedAdvisor?> AuthenticateAsync(string? userName, string? password, CancellationToken ct = default);
}
