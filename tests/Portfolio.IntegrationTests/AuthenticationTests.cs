using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Portfolio.IntegrationTests;

[TestFixture]
public class AuthenticationTests
{
    private PortfolioApiFactory _factory = null!;

    [OneTimeSetUp]
    public void StartApi() => _factory = new PortfolioApiFactory();

    [OneTimeTearDown]
    public void StopApi() => _factory.Dispose();

    [Test]
    public async Task Valid_credentials_return_a_token()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("amartin");

        var response = await client.GetAsync("/api/clients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("amartin", "mauvais-mot-de-passe")]
    [TestCase("inconnu", "Demo-2026!")]
    [TestCase("", "")]
    public async Task Invalid_credentials_are_rejected_with_the_same_message(string userName, string password)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { userName, password });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        Assert.That(problem!.Title, Is.EqualTo("Identifiant ou mot de passe incorrect."));
    }

    [TestCase("/api/clients")]
    [TestCase("/api/clients/1")]
    [TestCase("/api/clients/1/valuation")]
    [TestCase("/api/currencies")]
    public async Task Protected_endpoints_require_a_token(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task A_forged_token_is_rejected()
    {
        var client = _factory.CreateClient();
        // En-tête et contenu valides en apparence, signature arbitraire.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiaXNzIjoibWluaS1wb3J0ZWZldWlsbGUiLCJhdWQiOiJtaW5pLXBvcnRlZmV1aWxsZS13ZWIifQ.c2lnbmF0dXJlLWZhdXNzZQ");

        var response = await client.GetAsync("/api/clients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
