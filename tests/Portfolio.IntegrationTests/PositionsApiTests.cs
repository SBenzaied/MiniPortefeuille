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
public sealed class PositionsApiTests : IAsyncLifetime
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = PortfolioApiFactory.Json;

    private readonly PortfolioApiFactory _factory = new();
    private HttpClient _alice = null!;
    private int _kellerId;

    public async Task InitializeAsync()
    {
        _alice = await _factory.CreateAuthenticatedClientAsync("amartin");
        var clients = await _alice.GetFromJsonAsync<List<ClientSummary>>("/api/clients", Json);
        _kellerId = clients!.Single(c => c.Name == "Sophie Keller").Id;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static object NewPosition(string instrument = "Swisscom AG", decimal quantity = 20) => new
    {
        instrument,
        assetClass = "Equities",
        quantity,
        unitPrice = 520.5m,
        currency = "CHF",
    };

    [Fact]
    public async Task Position_lifecycle_create_update_delete_is_reflected_in_detail_and_audit()
    {
        // Création
        var created = await _alice.PostAsJsonAsync($"/api/clients/{_kellerId}/positions", NewPosition());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var position = (await created.Content.ReadFromJsonAsync<Position>(Json))!;
        Assert.Equal((20m, 10_410m), (position.Quantity, position.MarketValue));

        // Modification
        var updated = await _alice.PutAsJsonAsync($"/api/clients/{_kellerId}/positions/{position.Id}", NewPosition(quantity: 25));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var detail = await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json);
        Assert.Equal(25m, detail!.Positions.Single(p => p.Id == position.Id).Quantity);

        // Suppression
        var deleted = await _alice.DeleteAsync($"/api/clients/{_kellerId}/positions/{position.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        detail = await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json);
        Assert.DoesNotContain(detail!.Positions, p => p.Id == position.Id);

        // Journal d'audit : trois entrées, dans l'ordre, avec l'état avant/après.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var audit = await db.PositionAudit.Where(a => a.PositionId == position.Id).OrderBy(a => a.Id).ToListAsync();
        Assert.Equal([AuditAction.Created, AuditAction.Updated, AuditAction.Deleted], audit.Select(a => a.Action));
        Assert.Null(audit[0].Before);
        Assert.Contains("20 x 520.5 CHF", audit[1].Before);
        Assert.Contains("25 x 520.5 CHF", audit[1].After);
        Assert.Null(audit[2].After);
    }

    [Fact]
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

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblem>())!;
        Assert.Equal("Les données envoyées ne sont pas valides.", problem.Title);
        Assert.Equal(["assetClass", "currency", "instrument", "quantity"], problem.Errors.Keys.Order());
        Assert.Equal("La quantité doit être strictement positive.", problem.Errors["quantity"].Single());
    }

    [Fact]
    public async Task Malformed_json_value_is_reported_on_the_field_without_technical_details()
    {
        var body = new StringContent("""{"instrument":"X","assetClass":"Bonds","quantity":"abc","unitPrice":1,"currency":"CHF"}""",
            Encoding.UTF8, "application/json");

        var response = await _alice.PostAsync($"/api/clients/{_kellerId}/positions", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblem>())!;
        Assert.Equal(["quantity"], problem.Errors.Keys);
        Assert.DoesNotContain("Portfolio.", problem.Errors["quantity"].Single());
    }

    [Fact]
    public async Task Positions_of_another_advisors_client_cannot_be_touched()
    {
        var bruno = await _factory.CreateAuthenticatedClientAsync("bnguyen");

        var create = await bruno.PostAsJsonAsync($"/api/clients/{_kellerId}/positions", NewPosition());
        var kellerPositionId = (await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json))!.Positions[0].Id;
        var update = await bruno.PutAsJsonAsync($"/api/clients/{_kellerId}/positions/{kellerPositionId}", NewPosition());
        var delete = await bruno.DeleteAsync($"/api/clients/{_kellerId}/positions/{kellerPositionId}");

        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var detail = await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json);
        Assert.Equal(6, detail!.Positions.Count);
    }

    [Fact]
    public async Task A_position_cannot_be_moved_through_another_clients_url()
    {
        var clients = await _alice.GetFromJsonAsync<List<ClientSummary>>("/api/clients", Json);
        var duboisId = clients!.Single(c => c.Name == "Famille Dubois").Id;
        var kellerPositionId = (await _alice.GetFromJsonAsync<ClientDetail>($"/api/clients/{_kellerId}", Json))!.Positions[0].Id;

        // Même conseiller, mais la position n'appartient pas au client de l'URL.
        var response = await _alice.DeleteAsync($"/api/clients/{duboisId}/positions/{kellerPositionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
