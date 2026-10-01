using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ModelContextProtocol;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Mcp;
using Portfolio.Mcp.Tools;

namespace Portfolio.IntegrationTests;

/// <summary>
/// Chaîne complète du serveur MCP : outils → client REST → jeton du conseiller → API réelle en
/// mémoire. Seul le transport réseau est remplacé (le client HTTP parle au serveur de test).
/// </summary>
[TestFixture]
public class McpToolsTests
{
    private PortfolioApiFactory _api = null!;
    private ServiceProvider _mcpServices = null!;

    [OneTimeSetUp]
    public void Start()
    {
        _api = new PortfolioApiFactory();
        var apiHandler = _api.Server.CreateHandler();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PortfolioApi:BaseUrl"] = "http://localhost",
            ["PortfolioApi:UserName"] = "amartin",
            ["PortfolioApi:Password"] = DemoDataSeeder.DemoPassword,
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPortfolioApi(configuration);
        // Mêmes enregistrements qu'en production ; seule la connexion réseau pointe vers l'API de test.
        services.ConfigureAll<HttpClientFactoryOptions>(o => o.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = apiHandler));
        services.AddTransient<PortfolioTools>();
        _mcpServices = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [OneTimeTearDown]
    public void Stop()
    {
        _mcpServices.Dispose();
        _api.Dispose();
    }

    private PortfolioTools Tools => _mcpServices.GetRequiredService<PortfolioTools>();

    [Test]
    public async Task Lists_only_the_configured_advisors_clients()
    {
        var clients = await Tools.ListerClients();

        Assert.That(clients.Select(c => c.Nom), Is.EqualTo(new[] { "Famille Dubois", "Jean Rochat", "Marc Weber", "Sophie Keller" }));
        Assert.That(clients.Single(c => c.Nom == "Marc Weber").ClassesHorsProfil, Is.EquivalentTo(new[] { "Actions", "Liquidités" }));
    }

    [Test]
    public async Task Values_a_portfolio_in_another_currency_with_its_profile_bounds()
    {
        var rochat = (await Tools.ListerClients("prudent")).Single(c => c.Nom == "Jean Rochat");

        var valuation = await Tools.ValoriserPortefeuille(rochat.Id, "usd");

        Assert.That(valuation.Devise, Is.EqualTo("USD"));
        Assert.That(valuation.ValeurTotale, Is.EqualTo(91_887.50m));
        var equities = valuation.Repartition.Single(l => l.Classe == "Actions");
        Assert.That((equities.PartPourcent, equities.MaximumPourcent, equities.HorsProfil), Is.EqualTo((65.75m, (decimal?)30m, true)));
    }

    [Test]
    public async Task Another_advisors_client_is_not_found()
    {
        // Paul Meier appartient à Bruno : invisible pour le conseiller configuré (Alice).
        using var scope = _api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var meierId = db.Clients.Single(c => c.Name == "Paul Meier").Id;

        await Assert.ThrowsAsync<McpException>(() => Tools.ConsulterClient(meierId));
    }

    [Test]
    public async Task Lists_out_of_profile_clients_with_their_alert_messages()
    {
        var flagged = await Tools.ClientsHorsProfil();

        Assert.That(flagged.Select(c => c.Nom), Is.EquivalentTo(new[] { "Jean Rochat", "Marc Weber" }));
        Assert.That(flagged.Single(c => c.Nom == "Jean Rochat").Alertes.Single(), Does.StartWith("Actions : 65.75 %"));
    }

    [Test]
    public async Task Unknown_currency_message_comes_from_the_api()
    {
        var ex = await Assert.ThrowsAsync<McpException>(async () => await Tools.ValoriserPortefeuille(
            (await Tools.ListerClients()).First().Id, "JPY"));

        Assert.That(ex!.Message, Does.Contain("JPY"));
    }
}
