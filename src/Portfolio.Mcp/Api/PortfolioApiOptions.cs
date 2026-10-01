using System.ComponentModel.DataAnnotations;

namespace Portfolio.Mcp.Api;

/// <summary>
/// Accès à l'API REST de Mini-portefeuille. Le serveur MCP agit au nom d'un conseiller : il ne
/// voit que ses clients, comme dans l'interface web (cloisonnement appliqué par l'API).
/// </summary>
public sealed class PortfolioApiOptions
{
    public const string SectionName = "PortfolioApi";

    /// <summary>Adresse de l'API, par exemple <c>http://localhost:5215</c> ou l'URL Render.</summary>
    [Required, Url]
    public string BaseUrl { get; set; } = "";

    [Required]
    public string UserName { get; set; } = "";

    /// <summary>Jamais dans le dépôt : variable d'environnement <c>PortfolioApi__Password</c>.</summary>
    [Required]
    public string Password { get; set; } = "";
}
