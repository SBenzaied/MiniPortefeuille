using System.Net;
using System.Net.Http.Headers;

namespace Portfolio.Mcp.Api;

/// <summary>
/// Ajoute le jeton du conseiller à chaque requête vers l'API. Si l'API le refuse (jeton expiré,
/// clé de signature changée au redémarrage de l'API), il est renouvelé et la requête rejouée une fois.
/// </summary>
public sealed class AdvisorAuthenticationHandler(IAdvisorTokenProvider tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetTokenAsync(ct));
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        tokens.Invalidate();
        // Les requêtes de ce client sont des GET sans corps : elles peuvent être rejouées telles quelles.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetTokenAsync(ct));
        return await base.SendAsync(request, ct);
    }
}
