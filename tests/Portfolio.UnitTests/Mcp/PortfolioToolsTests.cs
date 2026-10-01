using ModelContextProtocol;
using Portfolio.Mcp.Api;
using Portfolio.Mcp.Tools;
using ApiValuation = Portfolio.Mcp.Api.Valuation;

namespace Portfolio.UnitTests.Mcp;

/// <summary>Outils MCP isolés de l'API REST : <see cref="IPortfolioApiClient"/> est simulé avec Moq.</summary>
[TestFixture]
public class PortfolioToolsTests
{
    private static readonly ClientSummary Rochat = new(2, "Jean Rochat", "Prudent", 4, 73_510m, 1, ["Equities"]);
    private static readonly ClientSummary Dubois = new(1, "Famille Dubois", "Prudent", 4, 92_488.5m, 0, []);

    private Mock<IPortfolioApiClient> _api = null!;
    private PortfolioTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _api = new Mock<IPortfolioApiClient>();
        _tools = new PortfolioTools(_api.Object);
    }

    [TestCase("Équilibré", "Balanced")]
    [TestCase("equilibre", "Balanced")]
    [TestCase("DYNAMIQUE", "Dynamic")]
    [TestCase("Prudent", "Prudent")]
    public async Task Profile_filter_accepts_french_labels_and_passes_the_api_value(string input, string expected)
    {
        _api.Setup(a => a.ListClientsAsync(expected, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _tools.ListerClients(input);

        _api.Verify(a => a.ListClientsAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Unknown_profile_is_reported_to_the_model_without_calling_the_api()
    {
        var ex = await Assert.ThrowsAsync<McpException>(() => _tools.ListerClients("Agressif"));

        Assert.That(ex!.Message, Does.Contain("Prudent, Équilibré, Dynamique"));
        _api.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Clients_are_returned_with_french_labels()
    {
        _api.Setup(a => a.ListClientsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync([Rochat]);

        var clients = await _tools.ListerClients();

        Assert.That(clients, Is.EqualTo(new[] { new ClientResume(2, "Jean Rochat", "Prudent", 4, 73_510m, ["Actions"]) }).Using<ClientResume>(SameClient));
    }

    [Test]
    public async Task Valuation_marks_the_breached_asset_class_and_keeps_the_profile_bounds()
    {
        _api.Setup(a => a.GetValuationAsync(2, "EUR", It.IsAny<CancellationToken>())).ReturnsAsync(new ApiValuation("EUR", 78_202.13m,
            [new("Equities", 51_415.96m, 65.75m), new("Bonds", 16_148.94m, 20.65m)],
            [],
            [new("Equities", "AboveMaximum", 65.75m, 30m, "Actions : 65.75 % du portefeuille, au-dessus du maximum de 30 % pour un profil prudent.")],
            [new("Equities", null, 30m)]));

        var valuation = await _tools.ValoriserPortefeuille(2, "EUR");

        Assert.That(valuation.Devise, Is.EqualTo("EUR"));
        Assert.That(valuation.Repartition[0], Is.EqualTo(new LigneRepartition("Actions", 51_415.96m, 65.75m, null, 30m, HorsProfil: true)));
        Assert.That(valuation.Repartition[1].HorsProfil, Is.False);
        Assert.That(valuation.Alertes, Has.Count.EqualTo(1));
        Assert.That(valuation.Remarque, Does.Contain("fictives"));
    }

    [Test]
    public async Task Unknown_client_gives_the_same_message_whoever_owns_it()
    {
        _api.Setup(a => a.GetValuationAsync(99, "CHF", It.IsAny<CancellationToken>())).ReturnsAsync((ApiValuation?)null);

        var ex = await Assert.ThrowsAsync<McpException>(() => _tools.ValoriserPortefeuille(99));

        Assert.That(ex!.Message, Does.Contain("lister_clients"));
    }

    [Test]
    public async Task Api_validation_error_is_forwarded_to_the_model()
    {
        _api.Setup(a => a.GetValuationAsync(2, "JPY", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PortfolioApiException("Devise de référence inconnue : JPY."));

        var ex = await Assert.ThrowsAsync<McpException>(() => _tools.ValoriserPortefeuille(2, "JPY"));

        Assert.That(ex!.Message, Is.EqualTo("Devise de référence inconnue : JPY."));
    }

    [Test]
    public async Task Out_of_profile_clients_are_valued_and_the_others_are_skipped()
    {
        _api.Setup(a => a.ListClientsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync([Dubois, Rochat]);
        _api.Setup(a => a.GetValuationAsync(2, "CHF", It.IsAny<CancellationToken>())).ReturnsAsync(new ApiValuation("CHF", 73_510m, [], [],
            [new("Equities", "AboveMaximum", 65.75m, 30m, "Actions trop élevées")], []));

        var flagged = await _tools.ClientsHorsProfil();

        Assert.That(flagged.Select(c => (c.Nom, c.Alertes.Single())), Is.EqualTo(new[] { ("Jean Rochat", "Actions trop élevées") }));
        _api.Verify(a => a.GetValuationAsync(Dubois.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static bool SameClient(ClientResume a, ClientResume b) =>
        a with { ClassesHorsProfil = [] } == b with { ClassesHorsProfil = [] } && a.ClassesHorsProfil.SequenceEqual(b.ClassesHorsProfil);
}
