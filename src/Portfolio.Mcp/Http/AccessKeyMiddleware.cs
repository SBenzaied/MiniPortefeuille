using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Portfolio.Mcp.Http;

public sealed class McpAccessOptions
{
    public const string SectionName = "Mcp";

    /// <summary>
    /// Clé exigée pour appeler <c>/mcp</c> en HTTP. Jamais dans le dépôt : variable
    /// d'environnement <c>Mcp__AccessKey</c> (générée par Render).
    /// </summary>
    public string? AccessKey { get; set; }
}

/// <summary>
/// Protège le point d'accès MCP HTTP : sans cela, quiconque connaît l'URL lirait les portefeuilles
/// du conseiller configuré. La clé est acceptée dans l'en-tête <c>Authorization: Bearer</c>
/// (Claude Code, Claude Desktop) ou dans le paramètre <c>?key=</c> de l'URL, seule option pour
/// un connecteur personnalisé sur claude.ai sans OAuth.
/// </summary>
public sealed class AccessKeyMiddleware(RequestDelegate next, IOptions<McpAccessOptions> options)
{
    private readonly byte[] _expected = Encoding.UTF8.GetBytes(options.Value.AccessKey ?? "");

    public Task InvokeAsync(HttpContext context)
    {
        if (_expected.Length == 0 || IsAuthorized(context.Request))
        {
            return next(context);
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";
        return Task.CompletedTask;
    }

    private bool IsAuthorized(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        var provided = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : request.Query["key"].ToString();

        // Comparaison en temps constant : la durée ne révèle pas le nombre de caractères corrects.
        return provided.Length > 0 && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), _expected);
    }
}
