using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Portfolio.Mcp.Api;

/// <summary>Lecture des données du conseiller connecté via l'API REST.</summary>
public interface IPortfolioApiClient
{
    /// <param name="riskProfile">Valeur de l'API (<c>Prudent</c>, <c>Balanced</c>, <c>Dynamic</c>) ou <c>null</c>.</param>
    Task<IReadOnlyList<ClientSummary>> ListClientsAsync(string? riskProfile, CancellationToken ct = default);

    /// <returns><c>null</c> si le client n'existe pas ou n'appartient pas au conseiller.</returns>
    Task<ClientDetail?> GetClientAsync(int clientId, CancellationToken ct = default);

    /// <returns><c>null</c> si le client n'existe pas ou n'appartient pas au conseiller.</returns>
    /// <exception cref="PortfolioApiException">Devise inconnue ou autre requête refusée par l'API.</exception>
    Task<Valuation?> GetValuationAsync(int clientId, string currency, CancellationToken ct = default);

    Task<IReadOnlyList<CurrencyDto>> ListCurrenciesAsync(CancellationToken ct = default);
}

/// <summary>Erreur renvoyée par l'API (400 ProblemDetails), avec un message lisible.</summary>
public sealed class PortfolioApiException(string message) : Exception(message);

/// <summary>Client HTTP typé ; le jeton est ajouté par <see cref="AdvisorAuthenticationHandler"/>.</summary>
public sealed class PortfolioApiClient(HttpClient http) : IPortfolioApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<IReadOnlyList<ClientSummary>> ListClientsAsync(string? riskProfile, CancellationToken ct = default)
    {
        var url = riskProfile is null ? "/api/clients" : $"/api/clients?riskProfile={Uri.EscapeDataString(riskProfile)}";
        return await http.GetFromJsonAsync<List<ClientSummary>>(url, Json, ct) ?? [];
    }

    public Task<ClientDetail?> GetClientAsync(int clientId, CancellationToken ct = default) =>
        GetOrNullAsync<ClientDetail>($"/api/clients/{clientId}", ct);

    public Task<Valuation?> GetValuationAsync(int clientId, string currency, CancellationToken ct = default) =>
        GetOrNullAsync<Valuation>($"/api/clients/{clientId}/valuation?currency={Uri.EscapeDataString(currency)}", ct);

    public async Task<IReadOnlyList<CurrencyDto>> ListCurrenciesAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<CurrencyDto>>("/api/currencies", Json, ct) ?? [];

    private async Task<T?> GetOrNullAsync<T>(string url, CancellationToken ct) where T : class
    {
        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(Json, ct);
            var details = problem?.Errors?.Values.SelectMany(m => m) ?? [];
            throw new PortfolioApiException(string.Join(" ", details.DefaultIfEmpty(problem?.Title ?? "Requête refusée par l'API.")));
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(Json, ct);
    }

    private sealed record ProblemBody(string? Title, Dictionary<string, string[]>? Errors);
}
