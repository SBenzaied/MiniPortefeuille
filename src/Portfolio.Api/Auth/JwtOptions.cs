using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Portfolio.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "mini-portefeuille";
    public string Audience { get; set; } = "mini-portefeuille-web";
    public int LifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Clé HMAC (au moins 32 caractères). Jamais dans le dépôt : fournie par variable
    /// d'environnement (<c>Jwt__SigningKey</c>) ou user-secrets.
    /// </summary>
    public string? SigningKey { get; set; }

    public SymmetricSecurityKey ResolvedKey { get; private set; } = null!;

    /// <summary>
    /// Hors production, une clé absente est remplacée par une clé aléatoire éphémère : l'application
    /// démarre en une commande, au prix d'une déconnexion des utilisateurs à chaque redémarrage.
    /// En production, une clé absente ou trop courte empêche le démarrage.
    /// </summary>
    public void ResolveKey(bool isProduction, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(SigningKey))
        {
            if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
            {
                throw new InvalidOperationException("Jwt:SigningKey doit contenir au moins 32 octets (256 bits).");
            }
            ResolvedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
            return;
        }

        if (isProduction)
        {
            throw new InvalidOperationException("Jwt:SigningKey n'est pas configurée.");
        }

        logger.LogWarning("Jwt:SigningKey absente : utilisation d'une clé éphémère générée au démarrage (hors production uniquement).");
        ResolvedKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
    }
}
