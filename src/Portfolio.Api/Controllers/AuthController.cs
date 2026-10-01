using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Api.Auth;
using Portfolio.Api.Http;
using Portfolio.Application.Auth;

namespace Portfolio.Api.Controllers;

public sealed record LoginRequest(string? UserName, string? Password);

public sealed record AdvisorDto(int Id, string UserName, string DisplayName);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, AdvisorDto Advisor);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, JwtTokenIssuer tokens) : ControllerBase
{
    /// <summary>Échange identifiant et mot de passe contre un jeton JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var advisor = await auth.AuthenticateAsync(request.UserName, request.Password, ct);
        if (advisor is null)
        {
            // Message identique que l'utilisateur existe ou non.
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Identifiant ou mot de passe incorrect.");
        }

        var token = tokens.Issue(advisor);
        return new LoginResponse(token.AccessToken, token.ExpiresAt, new AdvisorDto(advisor.Id, advisor.UserName, advisor.DisplayName));
    }
}
