using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Portfolio.Mcp.Api;

/// <summary>Jeton d'accès du conseiller configuré, pour appeler l'API REST.</summary>
public interface IAdvisorTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken ct = default);

    /// <summary>Oublie le jeton en cache (refusé par l'API, par exemple après un redémarrage).</summary>
    void Invalidate();
}

/// <summary>
/// Se connecte via <c>POST /api/auth/login</c> et garde le jeton jusqu'à une minute avant son
/// expiration. Singleton : un seul jeton partagé par tous les appels d'outils.
/// </summary>
public sealed class AdvisorTokenProvider(
    IHttpClientFactory httpClients,
    IOptions<PortfolioApiOptions> options,
    TimeProvider clock) : IAdvisorTokenProvider
{
    public const string HttpClientName = "portfolio-auth";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private (string Token, DateTimeOffset RenewAt)? _cached;

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (_cached is { } cached && clock.GetUtcNow() < cached.RenewAt)
        {
            return cached.Token;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_cached is { } fresh && clock.GetUtcNow() < fresh.RenewAt)
            {
                return fresh.Token;
            }

            var settings = options.Value;
            var response = await httpClients.CreateClient(HttpClientName)
                .PostAsJsonAsync("/api/auth/login", new { userName = settings.UserName, password = settings.Password }, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException(
                    $"Connexion refusée par l'API pour « {settings.UserName} » : vérifier PortfolioApi:UserName et PortfolioApi:Password.");
            }
            response.EnsureSuccessStatusCode();

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(ct)
                ?? throw new InvalidOperationException("Réponse de connexion vide.");
            _cached = (login.AccessToken, login.ExpiresAt.AddMinutes(-1));
            return login.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _cached = null;
}
