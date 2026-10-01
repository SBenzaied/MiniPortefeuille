using System.Net;
using System.Net.Http.Json;
using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;

namespace Portfolio.IntegrationTests;

public class ClientsApiTests(PortfolioApiFactory factory) : IClassFixture<PortfolioApiFactory>
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = PortfolioApiFactory.Json;

    private async Task<List<ClientSummary>> ListAsync(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<List<ClientSummary>>($"/api/clients{query}", Json))!;

    [Fact]
    public async Task An_advisor_only_sees_their_own_clients()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");
        var bruno = await factory.CreateAuthenticatedClientAsync("bnguyen");

        var aliceClients = (await ListAsync(alice)).Select(c => c.Name);
        var brunoClients = (await ListAsync(bruno)).Select(c => c.Name);

        Assert.Equal(["Famille Dubois", "Jean Rochat", "Marc Weber", "Sophie Keller"], aliceClients);
        Assert.Equal(["Claire Favre", "Fondation Lémanique", "Paul Meier"], brunoClients);
    }

    [Fact]
    public async Task Clients_can_be_filtered_by_risk_profile()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");

        var prudent = await ListAsync(alice, "?riskProfile=Prudent");

        Assert.Equal(["Famille Dubois", "Jean Rochat"], prudent.Select(c => c.Name));
        Assert.All(prudent, c => Assert.Equal(RiskProfile.Prudent, c.RiskProfile));
    }

    [Fact]
    public async Task Unknown_risk_profile_filter_is_a_bad_request()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");

        var response = await alice.GetAsync("/api/clients?riskProfile=Agressif");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Client_detail_lists_positions()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var detail = await alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{rochat.Id}", Json);

        Assert.Equal("Alice Martin", detail!.AdvisorName);
        Assert.Equal(4, detail.Positions.Count);
        var apple = detail.Positions.Single(p => p.Instrument == "Apple Inc.");
        Assert.Equal((120m, 215m, "USD", 25_800m), (apple.Quantity, apple.UnitPrice, apple.Currency, apple.MarketValue));
    }

    [Fact]
    public async Task Valuation_computes_total_allocation_and_alerts_in_chf()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var valuation = await alice.GetFromJsonAsync<Valuation>($"/api/clients/{rochat.Id}/valuation", Json);

        // Novartis 300 × 92.30 = 27 690 CHF ; Apple 120 × 215 USD × 0.80 = 20 640 CHF ;
        // Confédération 150 × 101.20 = 15 180 CHF ; compte courant 10 000 CHF.
        Assert.Equal("CHF", valuation!.ReferenceCurrency);
        Assert.Equal(73_510m, valuation.TotalValue);
        var equities = valuation.Allocations.Single(a => a.AssetClass == AssetClass.Equities);
        Assert.Equal(48_330m, equities.Amount);
        Assert.Equal(65.75m, equities.Percentage); // 48 330 / 73 510 = 65.746…

        var alert = Assert.Single(valuation.Alerts);
        Assert.Equal((AssetClass.Equities, AlertKind.AboveMaximum, 30m), (alert.AssetClass, alert.Kind, alert.LimitPercentage));
    }

    [Fact]
    public async Task Valuation_can_use_another_reference_currency()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var valuation = await alice.GetFromJsonAsync<Valuation>($"/api/clients/{rochat.Id}/valuation?currency=usd", Json);

        Assert.Equal("USD", valuation!.ReferenceCurrency);
        Assert.Equal(91_887.50m, valuation.TotalValue); // 73 510 / 0.80
    }

    [Fact]
    public async Task Unknown_reference_currency_returns_a_validation_error()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var response = await alice.GetAsync($"/api/clients/{rochat.Id}/valuation?currency=JPY");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        Assert.Contains("currency", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Another_advisors_client_is_reported_as_not_found()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");
        var bruno = await factory.CreateAuthenticatedClientAsync("bnguyen");
        var brunosClientId = (await ListAsync(bruno)).First().Id;

        // 404 et non 403 : on ne confirme pas l'existence d'un client hors périmètre.
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/clients/{brunosClientId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/clients/{brunosClientId}/valuation")).StatusCode);
    }

    [Fact]
    public async Task Portfolio_can_be_exported_as_csv_by_its_advisor_only()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("amartin");
        var bruno = await factory.CreateAuthenticatedClientAsync("bnguyen");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var response = await alice.GetAsync($"/api/clients/{rochat.Id}/export.csv?currency=CHF");
        var forbidden = await bruno.GetAsync($"/api/clients/{rochat.Id}/export.csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Apple Inc.;Equities;120;215;USD;25800;20640", csv);
        Assert.Contains("Total;;;;;;73510", csv);
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
    }

    [Fact]
    public async Task Empty_portfolio_is_valued_at_zero_without_alert()
    {
        var bruno = await factory.CreateAuthenticatedClientAsync("bnguyen");
        var meier = (await ListAsync(bruno)).Single(c => c.Name == "Paul Meier");

        var valuation = await bruno.GetFromJsonAsync<Valuation>($"/api/clients/{meier.Id}/valuation", Json);

        Assert.Equal(0m, valuation!.TotalValue);
        Assert.Empty(valuation.Alerts);
    }
}
