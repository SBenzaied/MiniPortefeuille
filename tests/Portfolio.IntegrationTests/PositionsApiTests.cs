using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.IntegrationTests;

/// <summary>
/// Une fabrique (donc une base) par test : ces tests modifient les données.
/// </summary>
[TestFixture]
public sealed class PositionsApiTests
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = PortfolioApiFactory.Json;

    private PortfolioApiFactory _factory = null!;
    private HttpClient _alice = null!;
    private int _kellerId;

    [SetUp]
    public async Task StartApiWithFreshDatabase()
    {
        _factory = new PortfolioApiFactory();
        _alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var clients = await _alice.GetFromJsonAsync<List<ClientSummary>>("/api/clients", Json);
        _kellerId = clients!.Single(c => c.Name == "Sophie Keller").Id;
    }

    [TearDown]
    public void StopApi() => _factory.Dispose();

    private static object NewPosition(string instrument = "Swisscom AG", decimal quantity = 20) => new
    {
        instrument,
        assetClass = "Equities",
        quantity,
        unitPrice = 520.5m,
        currency = "CHF",
    };

    [Test]
    public async Task Position_lifecycle_create_update_delete_is_reflected_in_detail_and_audit()
    {
        // Création
        var created = await _alice.PostAsJsonAsync($"/api/clients/{_kellerId}/positions", NewPosition());
        Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var position = (await created.Content.ReadFromJsonAsync<Position>(Json))!;
        Assert.That((position.Quantity, position.MarketValue), Is.EqualTo((20m, 10_410m)));

        // Modification
        var updated = await _alice.PutAsJsonAsync($"/api/clients/{_kellerId}/positions/{position.Id}", NewPosition(quantity: 25));
        Assert.That(updated.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json);
        Assert.That(detail!.Positions.Single(p => p.Id == position.Id).Quantity, Is.EqualTo(25m));

        // Suppression
        var deleted = await _alice.DeleteAsync($"/api/clients/{_kellerId}/positions/{position.Id}");
        Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        detail = await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json);
        Assert.That(detail!.Positions.Select(p => p.Id), Does.Not.Contain(position.Id));

        // Journal d'audit : trois entrées, dans l'ordre, avec l'état avant/après.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var audit = await db.PositionAudit.Where(a => a.PositionId == position.Id).OrderBy(a => a.Id).ToListAsync();
        Assert.That(audit.Select(a => a.Action), Is.EqualTo(new[] { AuditAction.Created, AuditAction.Updated, AuditAction.Deleted }));
        Assert.That(audit[0].Before, Is.Null);
        Assert.That(audit[1].Before, Does.Contain("20 x 520.5 CHF"));
        Assert.That(audit[1].After, Does.Contain("25 x 520.5 CHF"));
        Assert.That(audit[2].After, Is.Null);
    }

    [Test]
    public async Task Invalid_position_returns_field_errors()
    {
        var response = await _alice.PostAsJsonAsync($"/api/clients/{_kellerId}/positions", new
        {
            instrument = "",
            assetClass = "Crypto",
            quantity = -3,
            unitPrice = 10,
            currency = "JPY",
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblem>())!;
        Assert.That(problem.Title, Is.EqualTo("Les données envoyées ne sont pas valides."));
        Assert.That(problem.Errors.Keys, Is.EquivalentTo(new[] { "assetClass", "currency", "instrument", "quantity" }));
        Assert.That(problem.Errors["quantity"], Is.EqualTo(new[] { "La quantité doit être strictement positive." }));
    }

    [Test]
    public async Task Malformed_json_value_is_reported_on_the_field_without_technical_details()
    {
        var body = new StringContent("""{"instrument":"X","assetClass":"Bonds","quantity":"abc","unitPrice":1,"currency":"CHF"}""",
            Encoding.UTF8, "application/json");

        var response = await _alice.PostAsync($"/api/clients/{_kellerId}/positions", body);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblem>())!;
        Assert.That(problem.Errors.Keys, Is.EqualTo(new[] { "quantity" }));
        Assert.That(problem.Errors["quantity"].Single(), Does.Not.Contain("Portfolio."));
    }

    [Test]
    public async Task Positions_of_another_advisors_client_cannot_be_touched()
    {
        var bruno = await _factory.CreateAuthenticatedClientAsync("bnguyen");

        var create = await bruno.PostAsJsonAsync($"/api/clients/{_kellerId}/positions", NewPosition());
        var kellerPositionId = (await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json))!.Positions[0].Id;
        var update = await bruno.PutAsJsonAsync($"/api/clients/{_kellerId}/positions/{kellerPositionId}", NewPosition());
        var delete = await bruno.DeleteAsync($"/api/clients/{_kellerId}/positions/{kellerPositionId}");

        Assert.That(new[] { create.StatusCode, update.StatusCode, delete.StatusCode }, Is.All.EqualTo(HttpStatusCode.NotFound));
        var detail = await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json);
        Assert.That(detail!.Positions, Has.Count.EqualTo(6));
    }

    [Test]
    public async Task A_position_cannot_be_moved_through_another_clients_url()
    {
        var clients = await _alice.GetFromJsonAsync<List<ClientSummary>>("/api/clients", Json);
        var duboisId = clients!.Single(c => c.Name == "Famille Dubois").Id;
        var kellerPositionId = (await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json))!.Positions[0].Id;

        // Même conseiller, mais la position n'appartient pas au client de l'URL.
        var response = await _alice.DeleteAsync($"/api/clients/{duboisId}/positions/{kellerPositionId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
