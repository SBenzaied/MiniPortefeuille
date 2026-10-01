using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.IntegrationTests;

/// <summary>
/// Démarre l'API complète en mémoire (pipeline HTTP, JWT, EF Core) sur une base SQLite en
/// mémoire, propre à chaque instance de la fabrique et chargée avec le jeu d'essai.
/// </summary>
public sealed class PortfolioApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _connectionString = $"Data Source=tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public PortfolioApiFactory()
    {
        // Une base SQLite en mémoire disparaît à la fermeture de sa dernière connexion :
        // celle-ci reste ouverte pendant toute la durée de vie de la fabrique.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:SeedDemoData", "true");
        builder.UseSetting("ConnectionStrings:Portfolio", _connectionString);
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("RateLimiting:LoginPermitPerMinute", "1000");
    }

    public Task<HttpClient> CreateAuthenticatedClientAsync(string userName) => AuthenticateAsync(CreateClient(), userName);

    /// <summary>Connecte un client HTTP (y compris d'une fabrique dérivée par WithWebHostBuilder).</summary>
    public static async Task<HttpClient> AuthenticateAsync(HttpClient client, string userName)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = DemoDataSeeder.DemoPassword });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
