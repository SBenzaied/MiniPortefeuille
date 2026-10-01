using Npgsql;

namespace Portfolio.Infrastructure.Persistence;

/// <summary>
/// Accepte une chaîne de connexion Npgsql classique (<c>Host=…;Database=…</c>) ou une URL
/// <c>postgres://utilisateur:motdepasse@hôte:port/base</c>, format fourni par les hébergeurs
/// comme Render ou Heroku et que Npgsql ne lit pas directement.
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!Uri.TryCreate(connectionString.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("postgres" or "postgresql"))
        {
            return connectionString;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            // Chiffre si le serveur le propose (URL externe de Render), sans l'exiger (URL interne).
            SslMode = SslMode.Prefer,
        };

        foreach (var (key, value) in ParseQuery(uri.Query))
        {
            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<SslMode>(value, ignoreCase: true, out var mode))
            {
                builder.SslMode = mode;
            }
        }

        return builder.ConnectionString;
    }

    private static IEnumerable<(string Key, string Value)> ParseQuery(string query) =>
        query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Select(kv => (Uri.UnescapeDataString(kv[0]), kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : ""));
}
