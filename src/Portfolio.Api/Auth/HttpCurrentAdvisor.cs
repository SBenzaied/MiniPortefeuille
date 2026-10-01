using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Portfolio.Application.Abstractions;

namespace Portfolio.Api.Auth;

/// <summary>
/// Conseiller connecté, lu dans le claim <c>sub</c> du jeton de la requête en cours. Enregistré
/// en scoped : une instance par requête HTTP, injectée dans les services applicatifs.
/// </summary>
public sealed class HttpCurrentAdvisor(IHttpContextAccessor httpContextAccessor) : ICurrentAdvisor
{
    public int Id
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(sub, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? id
                : throw new InvalidOperationException("Jeton sans identifiant de conseiller valide.");
        }
    }
}
