using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Portfolio.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Identifiant du conseiller connecté, lu dans le claim <c>sub</c> du jeton. C'est la seule
    /// source de l'identité du conseiller : elle n'est jamais acceptée depuis l'URL ou le corps.
    /// </summary>
    public static int GetAdvisorId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(sub, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new InvalidOperationException("Jeton sans identifiant de conseiller valide.");
    }
}
