using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Portfolio.IntegrationTests;

public class AuthenticationTests(PortfolioApiFactory factory) : IClassFixture<PortfolioApiFactory>
{
    [Fact]
    public async Task Valid_credentials_return_a_token()
    {
        var client = await factory.CreateAuthenticatedClientAsync("amartin");

        var response = await client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("amartin", "mauvais-mot-de-passe")]
    [InlineData("inconnu", "Demo-2026!")]
    [InlineData("", "")]
    public async Task Invalid_credentials_are_rejected_with_the_same_message(string userName, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { userName, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        Assert.Equal("Identifiant ou mot de passe incorrect.", problem!.Title);
    }

    [Theory]
    [InlineData("/api/clients")]
    [InlineData("/api/clients/1")]
    [InlineData("/api/clients/1/valuation")]
    [InlineData("/api/currencies")]
    public async Task Protected_endpoints_require_a_token(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_forged_token_is_rejected()
    {
        var client = factory.CreateClient();
        // En-tête et contenu valides en apparence, signature arbitraire.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiaXNzIjoibWluaS1wb3J0ZWZldWlsbGUiLCJhdWQiOiJtaW5pLXBvcnRlZmV1aWxsZS13ZWIifQ.c2lnbmF0dXJlLWZhdXNzZQ");

        var response = await client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
