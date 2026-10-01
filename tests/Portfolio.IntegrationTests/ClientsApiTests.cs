using System.Net;
using System.Net.Http.Json;
using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;

namespace Portfolio.IntegrationTests;

/// <summary>Lecture seule : une seule API (donc une seule base) pour toute la fixture.</summary>
[TestFixture]
public class ClientsApiTests
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = PortfolioApiFactory.Json;

    private PortfolioApiFactory _factory = null!;

    [OneTimeSetUp]
    public void StartApi() => _factory = new PortfolioApiFactory();

    [OneTimeTearDown]
    public void StopApi() => _factory.Dispose();

    private static async Task<List<ClientSummary>> ListAsync(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<List<ClientSummary>>($"/api/clients{query}", Json))!;

    [Test]
    public async Task An_advisor_only_sees_their_own_clients()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var bruno = await _factory.CreateAuthenticatedClientAsync("bnguyen");

        var aliceClients = (await ListAsync(alice)).Select(c => c.Name);
        var brunoClients = (await ListAsync(bruno)).Select(c => c.Name);

        Assert.That(aliceClients, Is.EqualTo(new[] { "Famille Dubois", "Jean Rochat", "Marc Weber", "Sophie Keller" }));
        Assert.That(brunoClients, Is.EqualTo(new[] { "Claire Favre", "Fondation Lémanique", "Paul Meier" }));
    }

    [Test]
    public async Task Clients_can_be_filtered_by_risk_profile()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");

        var prudent = await ListAsync(alice, "?riskProfile=Prudent");

        Assert.That(prudent.Select(c => c.Name), Is.EqualTo(new[] { "Famille Dubois", "Jean Rochat" }));
        Assert.That(prudent.Select(c => c.RiskProfile), Is.All.EqualTo(RiskProfile.Prudent));
    }

    [Test]
    public async Task Unknown_risk_profile_filter_is_a_bad_request()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");

        var response = await alice.GetAsync("/api/clients?riskProfile=Agressif");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Client_detail_lists_positions()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var detail = await alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{rochat.Id}", Json);

        Assert.That(detail!.AdvisorName, Is.EqualTo("Alice Martin"));
        Assert.That(detail.Positions, Has.Count.EqualTo(4));
        var apple = detail.Positions.Single(p => p.Instrument == "Apple Inc.");
        Assert.That((apple.Quantity, apple.UnitPrice, apple.Currency, apple.MarketValue), Is.EqualTo((120m, 215m, "USD", 25_800m)));
    }

    [Test]
    public async Task List_names_the_asset_classes_out_of_profile()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var clients = await ListAsync(alice);

        Assert.That(clients.Single(c => c.Name == "Jean Rochat").AlertAssetClasses, Is.EqualTo(new[] { AssetClass.Equities }));
        Assert.That(clients.Single(c => c.Name == "Famille Dubois").AlertAssetClasses, Is.Empty);
    }

    [Test]
    public async Task Valuation_computes_total_allocation_and_alerts_in_chf()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var valuation = await alice.GetFromJsonAsync<Valuation>($"/api/clients/{rochat.Id}/valuation", Json);

        // Novartis 300 × 92.30 = 27 690 CHF ; Apple 120 × 215 USD × 0.80 = 20 640 CHF ;
        // Confédération 150 × 101.20 = 15 180 CHF ; compte courant 10 000 CHF.
        Assert.That(valuation!.ReferenceCurrency, Is.EqualTo("CHF"));
        Assert.That(valuation.TotalValue, Is.EqualTo(73_510m));
        var equities = valuation.Allocations.Single(a => a.AssetClass == AssetClass.Equities);
        Assert.That(equities.Amount, Is.EqualTo(48_330m));
        Assert.That(equities.Percentage, Is.EqualTo(65.75m)); // 48 330 / 73 510 = 65.746…

        Assert.That(valuation.Alerts, Has.Count.EqualTo(1));
        var alert = valuation.Alerts[0];
        Assert.That((alert.AssetClass, alert.Kind, alert.LimitPercentage), Is.EqualTo((AssetClass.Equities, AlertKind.AboveMaximum, 30m)));

        // Les bornes du profil prudent sont exposées pour tracer la bande autorisée.
        var equityLimit = valuation.Limits.Single(l => l.AssetClass == AssetClass.Equities);
        Assert.That(equityLimit.MinPercentage, Is.Null);
        Assert.That(equityLimit.MaxPercentage, Is.EqualTo(30m));
    }

    [Test]
    public async Task Valuation_can_use_another_reference_currency()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var valuation = await alice.GetFromJsonAsync<Valuation>($"/api/clients/{rochat.Id}/valuation?currency=usd", Json);

        Assert.That(valuation!.ReferenceCurrency, Is.EqualTo("USD"));
        Assert.That(valuation.TotalValue, Is.EqualTo(91_887.50m)); // 73 510 / 0.80
    }

    [Test]
    public async Task Unknown_reference_currency_returns_a_validation_error()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var response = await alice.GetAsync($"/api/clients/{rochat.Id}/valuation?currency=JPY");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        Assert.That(problem!.Errors, Does.ContainKey("currency"));
    }

    [Test]
    public async Task Another_advisors_client_is_reported_as_not_found()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var bruno = await _factory.CreateAuthenticatedClientAsync("bnguyen");
        var brunosClientId = (await ListAsync(bruno)).First().Id;

        // 404 et non 403 : on ne confirme pas l'existence d'un client hors périmètre.
        Assert.That((await alice.GetAsync($"/api/clients/{brunosClientId}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await alice.GetAsync($"/api/clients/{brunosClientId}/valuation")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Portfolio_can_be_exported_as_csv_by_its_advisor_only()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var bruno = await _factory.CreateAuthenticatedClientAsync("bnguyen");
        var rochat = (await ListAsync(alice)).Single(c => c.Name == "Jean Rochat");

        var response = await alice.GetAsync($"/api/clients/{rochat.Id}/export.csv?currency=CHF");
        var forbidden = await bruno.GetAsync($"/api/clients/{rochat.Id}/export.csv");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("text/csv"));
        var csv = await response.Content.ReadAsStringAsync();
        Assert.That(csv, Does.Contain("Apple Inc.;Equities;120;215;USD;25800;20640"));
        Assert.That(csv, Does.Contain("Total;;;;;;73510"));
        Assert.That(forbidden.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Empty_portfolio_is_valued_at_zero_without_alert()
    {
        var bruno = await _factory.CreateAuthenticatedClientAsync("bnguyen");
        var meier = (await ListAsync(bruno)).Single(c => c.Name == "Paul Meier");

        var valuation = await bruno.GetFromJsonAsync<Valuation>($"/api/clients/{meier.Id}/valuation", Json);

        Assert.That(valuation!.TotalValue, Is.Zero);
        Assert.That(valuation.Alerts, Is.Empty);
    }
}
