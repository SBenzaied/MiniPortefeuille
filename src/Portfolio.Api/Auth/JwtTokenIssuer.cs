using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Application.Auth;

namespace Portfolio.Api.Auth;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>Émission du jeton d'accès d'un conseiller authentifié.</summary>
public interface ITokenIssuer
{
    IssuedToken Issue(AuthenticatedAdvisor advisor);
}

/// <summary>Jeton JWT HS256 signé avec la clé de configuration ; l'horloge est injectée.</summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : ITokenIssuer
{
    public IssuedToken Issue(AuthenticatedAdvisor advisor)
    {
        var jwt = options.Value;
        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.LifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.ResolvedKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, advisor.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.UniqueName, advisor.UserName),
                new Claim(JwtRegisteredClaimNames.Name, advisor.DisplayName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
        };

        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
