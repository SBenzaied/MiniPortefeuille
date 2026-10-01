using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portfolio.IntegrationTests;

/// <summary>
/// Comportements propres au déploiement en un seul service (Render) : front servi par l'API et
/// adresse du client lue dans l'en-tête X-Forwarded-For du proxy.
/// </summary>
[TestFixture]
public class DeploymentTests
{
    private PortfolioApiFactory _factory = null!;
    private WebApplicationFactory<Program> _withFrontend = null!;
    private string _webRoot = null!;

    [OneTimeSetUp]
    public void StartApi()
    {
        _factory = new PortfolioApiFactory();
        _webRoot = Directory.CreateTempSubdirectory("mini-portefeuille-wwwroot-").FullName;
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<!doctype html><div id=\"root\"></div>");
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", "index-abc123.js"), "console.log('ok')");
        _withFrontend = _factory.WithWebHostBuilder(b => b.UseSetting(WebHostDefaults.WebRootKey, _webRoot));
    }

    [OneTimeTearDown]
    public void StopApi()
    {
        _withFrontend.Dispose();
        _factory.Dispose();
        Directory.Delete(_webRoot, recursive: true);
    }

    private HttpClient ClientWithFrontend() => _withFrontend.CreateClient();

    [Test]
    public async Task Spa_routes_return_index_html_with_security_headers()
    {
        var response = await ClientWithFrontend().GetAsync("/clients/2");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("<div id=\"root\">"));
        Assert.That(response.Headers.GetValues("Content-Security-Policy").Single(), Does.StartWith("default-src 'self'"));
        Assert.That(response.Headers.GetValues("X-Frame-Options").Single(), Is.EqualTo("DENY"));
        Assert.That(response.Headers.CacheControl!.NoCache, Is.True);
    }

    [Test]
    public async Task Fingerprinted_assets_are_cached_for_a_year()
    {
        var response = await ClientWithFrontend().GetAsync("/assets/index-abc123.js");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.CacheControl!.MaxAge, Is.EqualTo(TimeSpan.FromDays(365)));
    }

    [Test]
    public async Task Unknown_api_route_stays_a_404_and_not_the_spa()
    {
        var response = await ClientWithFrontend().GetAsync("/api/inexistant");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo("text/html"));
    }

    [Test]
    public async Task Without_a_frontend_the_api_does_not_serve_html()
    {
        var response = await _factory.CreateClient().GetAsync("/clients/2");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Behind_a_proxy_the_login_limit_applies_per_client_address()
    {
        using var behindProxy = _factory.WithWebHostBuilder(b => b
            .UseSetting("ReverseProxy:TrustForwardedHeaders", "true")
            .UseSetting("RateLimiting:LoginPermitPerMinute", "2"));

        async Task<HttpStatusCode> LoginFrom(string forwardedFor)
        {
            var client = behindProxy.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
            var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = "amartin", password = "faux" });
            return response.StatusCode;
        }

        // Trois essais depuis la même adresse : le troisième est refusé.
        Assert.That(await LoginFrom("203.0.113.10"), Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await LoginFrom("203.0.113.10"), Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await LoginFrom("203.0.113.10"), Is.EqualTo(HttpStatusCode.TooManyRequests));

        // Une autre adresse a son propre quota, même si elle prétend être la première en tête de liste :
        // seule la dernière adresse (ajoutée par le proxy) est prise en compte.
        Assert.That(await LoginFrom("198.51.100.7"), Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await LoginFrom("203.0.113.10, 198.51.100.8"), Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
