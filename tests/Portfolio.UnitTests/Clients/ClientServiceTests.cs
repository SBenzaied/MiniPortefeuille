using Portfolio.Application.Abstractions;
using Portfolio.Application.Clients;
using Portfolio.Application.Common;
using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Clients;

/// <summary>
/// <see cref="ClientService"/> isolé de ses collaborateurs : taux, valorisation, politique et
/// alertes sont simulés avec Moq. On vérifie ce que le service leur transmet et ce qu'il fait
/// de leurs réponses, sans dépendre de leurs règles réelles.
/// </summary>
[TestFixture]
public class ClientServiceTests
{
    private static readonly ExchangeRates Rates = new(new Dictionary<string, decimal> { ["CHF"] = 1m, ["USD"] = 0.80m });

    private static readonly PortfolioValuation Valuation = new("CHF", 1_000m,
        [new(AssetClass.Equities, 1_000m, 100m), new(AssetClass.Bonds, 0m, 0m), new(AssetClass.Cash, 0m, 0m), new(AssetClass.Other, 0m, 0m)], []);

    private static readonly AllocationAlert EquitiesAlert = new(AssetClass.Equities, AlertKind.AboveMaximum, 100m, 30m, "Actions trop élevées");

    private TestDatabase _database = null!;
    private Mock<ICurrentAdvisor> _advisor = null!;
    private Mock<IExchangeRateProvider> _rates = null!;
    private Mock<IPortfolioValuator> _valuator = null!;
    private Mock<IAllocationPolicy> _policy = null!;
    private Mock<IAllocationAlertEvaluator> _alerts = null!;
    private IClientService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new TestDatabase();
        _advisor = new Mock<ICurrentAdvisor>();
        _advisor.Setup(a => a.Id).Returns(TestDatabase.AliceId);
        _rates = new Mock<IExchangeRateProvider>();
        _rates.Setup(r => r.GetRatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Rates);
        _valuator = new Mock<IPortfolioValuator>();
        _valuator.Setup(v => v.Value(It.IsAny<IEnumerable<Position>>(), It.IsAny<ExchangeRates>(), It.IsAny<string>())).Returns(Valuation);
        _policy = new Mock<IAllocationPolicy>();
        _policy.Setup(p => p.For(It.IsAny<RiskProfile>())).Returns([new AllocationLimit(AssetClass.Equities, null, 30m)]);
        _alerts = new Mock<IAllocationAlertEvaluator>();
        _alerts.Setup(a => a.Evaluate(It.IsAny<RiskProfile>(), It.IsAny<PortfolioValuation>())).Returns([EquitiesAlert]);

        _service = new ClientService(_database.Context, _advisor.Object, _rates.Object, _valuator.Object, _policy.Object, _alerts.Object);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task Valuation_combines_the_results_of_its_collaborators()
    {
        var result = await _service.GetValuationAsync(_database.Rochat.Id, "chf");

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        var dto = result.Value!;
        Assert.That(dto.TotalValue, Is.EqualTo(1_000m));
        Assert.That(dto.Alerts, Is.EqualTo(new[] { EquitiesAlert }));
        Assert.That(dto.Limits, Has.Count.EqualTo(1));

        // Les positions du client et les taux du fournisseur sont transmis au calcul, en devise normalisée.
        _valuator.Verify(v => v.Value(It.Is<IEnumerable<Position>>(p => p.Count() == 2), Rates, "CHF"), Times.Once);
        _alerts.Verify(a => a.Evaluate(RiskProfile.Prudent, Valuation), Times.Once);
        _policy.Verify(p => p.For(RiskProfile.Prudent), Times.Once);
    }

    [Test]
    public async Task Another_advisors_client_is_not_found_and_nothing_is_computed()
    {
        _advisor.Setup(a => a.Id).Returns(TestDatabase.BrunoId);

        var result = await _service.GetValuationAsync(_database.Rochat.Id, null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
        _valuator.VerifyNoOtherCalls();
        _alerts.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Unknown_reference_currency_is_rejected_before_any_computation()
    {
        var result = await _service.GetValuationAsync(_database.Rochat.Id, "JPY");

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Errors, Does.ContainKey("currency"));
        _valuator.VerifyNoOtherCalls();
    }

    [Test]
    public async Task List_only_returns_the_current_advisors_clients_with_their_alerts()
    {
        var aliceClients = await _service.ListAsync(riskProfile: null);

        Assert.That(aliceClients, Has.Count.EqualTo(1));
        Assert.That(aliceClients[0].Name, Is.EqualTo("Jean Rochat"));
        Assert.That(aliceClients[0].AlertCount, Is.EqualTo(1));
        Assert.That(aliceClients[0].AlertAssetClasses, Is.EqualTo(new[] { AssetClass.Equities }));

        _advisor.Setup(a => a.Id).Returns(TestDatabase.BrunoId);
        Assert.That(await _service.ListAsync(riskProfile: null), Is.Empty);
    }

    [Test]
    public async Task Rates_are_loaded_once_per_list_whatever_the_number_of_clients()
    {
        await _service.ListAsync(riskProfile: null);

        _rates.Verify(r => r.GetRatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
