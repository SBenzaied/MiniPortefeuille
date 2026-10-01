using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Abstractions;
using Portfolio.Application.Clients;
using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.IntegrationTests;

/// <summary>
/// Les services ne dépendent que d'interfaces : on peut en remplacer une dans le conteneur
/// (ici par un mock Moq) sans toucher au reste de l'application.
/// </summary>
[TestFixture]
public class DependencyInjectionTests
{
    private PortfolioApiFactory _factory = null!;

    [OneTimeSetUp]
    public void StartApi() => _factory = new PortfolioApiFactory();

    [OneTimeTearDown]
    public void StopApi() => _factory.Dispose();

    [Test]
    public void Every_interface_resolves_to_its_registered_implementation()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        Assert.That(services.GetRequiredService<IAllocationPolicy>(), Is.TypeOf<FixedAllocationPolicy>());
        Assert.That(services.GetRequiredService<IClientService>(), Is.TypeOf<ClientService>());
        Assert.That(services.GetRequiredService<IPortfolioExporter>(), Is.TypeOf<PortfolioCsvExporter>());
        Assert.That(services.GetRequiredService<IExchangeRateProvider>(), Is.TypeOf<DbExchangeRateProvider>());
    }

    [Test]
    public void Scoped_services_are_shared_within_a_request_and_distinct_across_requests()
    {
        using var first = _factory.Services.CreateScope();
        using var second = _factory.Services.CreateScope();

        Assert.That(first.ServiceProvider.GetRequiredService<IPortfolioDbContext>(),
            Is.SameAs(first.ServiceProvider.GetRequiredService<PortfolioDbContext>()), "le port et le DbContext sont la même instance");
        Assert.That(first.ServiceProvider.GetRequiredService<IClientService>(),
            Is.Not.SameAs(second.ServiceProvider.GetRequiredService<IClientService>()));
        Assert.That(first.ServiceProvider.GetRequiredService<IAllocationPolicy>(),
            Is.SameAs(second.ServiceProvider.GetRequiredService<IAllocationPolicy>()), "singleton");
    }

    [Test]
    public async Task Replacing_the_allocation_policy_with_a_mock_changes_alerts_without_touching_the_services()
    {
        // Politique simulée : plafonne les actions à 70 % pour tous les profils.
        var policy = new Mock<IAllocationPolicy>();
        policy.Setup(p => p.For(It.IsAny<RiskProfile>())).Returns([new AllocationLimit(AssetClass.Equities, null, 70m)]);

        using var withMockedPolicy = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton(policy.Object)));
        var alice = await PortfolioApiFactory.AuthenticateAsync(withMockedPolicy.CreateClient(), "amartin");

        var clients = (await alice.GetFromJsonAsync<List<ClientSummary>>("/api/clients", PortfolioApiFactory.Json))!;

        // Jean Rochat (65.75 % d'actions) dépasse 30 % avec la politique fixe, mais pas 70 %.
        Assert.That(clients.Single(c => c.Name == "Jean Rochat").AlertAssetClasses, Is.Empty);
        policy.Verify(p => p.For(RiskProfile.Prudent), Times.AtLeastOnce);
    }
}
