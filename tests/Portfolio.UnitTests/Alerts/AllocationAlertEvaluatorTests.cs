using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Alerts;

[TestFixture]
public class AllocationAlertEvaluatorTests
{
    // Sans conteneur : on construit l'évaluateur à la main en lui passant la politique réelle.
    private static readonly IAllocationAlertEvaluator Evaluator = new AllocationAlertEvaluator(new FixedAllocationPolicy());

    /// <summary>Construit une valorisation à partir des pourcentages voulus, sur un total de 100 CHF.</summary>
    private static PortfolioValuation Allocation(decimal equities = 0, decimal bonds = 0, decimal cash = 0, decimal other = 0)
    {
        var total = equities + bonds + cash + other;
        return new PortfolioValuation("CHF", total,
        [
            new(AssetClass.Equities, equities, equities),
            new(AssetClass.Bonds, bonds, bonds),
            new(AssetClass.Cash, cash, cash),
            new(AssetClass.Other, other, other),
        ], []);
    }

    [Test]
    public void Prudent_profile_with_more_than_30_percent_equities_raises_an_alert()
    {
        var alerts = Evaluator.Evaluate(RiskProfile.Prudent, Allocation(equities: 45, bonds: 50, cash: 5));

        Assert.That(alerts, Has.Count.EqualTo(1));
        Assert.That(alerts[0], Is.EqualTo(new AllocationAlert(AssetClass.Equities, AlertKind.AboveMaximum, 45m, 30m,
            "Actions : 45 % du portefeuille, au-dessus du maximum de 30 % pour un profil prudent.")));
    }

    [Test]
    public void Limit_itself_is_allowed()
    {
        Assert.That(Evaluator.Evaluate(RiskProfile.Prudent, Allocation(equities: 30, bonds: 70)), Is.Empty);
    }

    [Test]
    public void Comparison_uses_the_rounded_percentage_shown_to_the_advisor()
    {
        // 30.004 % est affiché 30.00 % par le calcul de valorisation : pas d'alerte.
        // On passe par le vrai calcul pour vérifier l'enchaînement valorisation → alerte.
        var rates = new ExchangeRates(new Dictionary<string, decimal> { ["CHF"] = 1m });
        Position P(AssetClass c, decimal value) => new() { Instrument = "x", AssetClass = c, Quantity = 1, UnitPrice = value, CurrencyCode = "CHF" };

        var valuation = new PortfolioValuator().Value([P(AssetClass.Equities, 30_004m), P(AssetClass.Bonds, 69_996m)], rates);

        Assert.That(valuation.For(AssetClass.Equities).Percentage, Is.EqualTo(30.00m));
        Assert.That(Evaluator.Evaluate(RiskProfile.Prudent, valuation), Is.Empty);
    }

    [Test]
    public void Balanced_profile_with_too_few_equities_raises_a_below_minimum_alert()
    {
        var alerts = Evaluator.Evaluate(RiskProfile.Balanced, Allocation(equities: 10, bonds: 80, cash: 10));

        Assert.That(alerts, Has.Count.EqualTo(1));
        Assert.That(alerts[0].Kind, Is.EqualTo(AlertKind.BelowMinimum));
        Assert.That(alerts[0].LimitPercentage, Is.EqualTo(20m));
    }

    [Test]
    public void Dynamic_profile_mostly_in_cash_raises_two_alerts()
    {
        var alerts = Evaluator.Evaluate(RiskProfile.Dynamic, Allocation(equities: 20, cash: 80));

        Assert.That(alerts.Select(a => (a.AssetClass, a.Kind)), Is.EquivalentTo(new[]
        {
            (AssetClass.Equities, AlertKind.BelowMinimum),
            (AssetClass.Cash, AlertKind.AboveMaximum),
        }));
    }

    [TestCase(RiskProfile.Prudent, 20, 70, 10, 0)]
    [TestCase(RiskProfile.Balanced, 45, 35, 10, 10)]
    [TestCase(RiskProfile.Dynamic, 85, 0, 5, 10)]
    public void Portfolio_in_line_with_its_profile_raises_no_alert(RiskProfile profile, decimal equities, decimal bonds, decimal cash, decimal other)
    {
        Assert.That(Evaluator.Evaluate(profile, Allocation(equities, bonds, cash, other)), Is.Empty);
    }

    [TestCase(RiskProfile.Prudent)]
    [TestCase(RiskProfile.Balanced)]
    [TestCase(RiskProfile.Dynamic)]
    public void Empty_portfolio_raises_no_alert(RiskProfile profile)
    {
        // Sinon un portefeuille vide déclencherait « actions sous le minimum » pour un profil dynamique.
        Assert.That(Evaluator.Evaluate(profile, Allocation()), Is.Empty);
    }

    [Test]
    public void Every_profile_has_a_policy()
    {
        var policy = new FixedAllocationPolicy();

        Assert.That(Enum.GetValues<RiskProfile>().Select(policy.For), Has.All.Not.Empty);
    }

    // --- Politique injectée, simulée avec Moq -------------------------------------------------

    [Test]
    public void Evaluator_applies_the_bounds_of_the_injected_policy()
    {
        var policy = new Mock<IAllocationPolicy>();
        policy.Setup(p => p.For(RiskProfile.Prudent)).Returns([new AllocationLimit(AssetClass.Bonds, MinPercentage: 60m, MaxPercentage: null)]);
        var evaluator = new AllocationAlertEvaluator(policy.Object);

        var alerts = evaluator.Evaluate(RiskProfile.Prudent, Allocation(equities: 45, bonds: 50, cash: 5));

        // Les 45 % d'actions ne sont plus en alerte : seule la borne simulée compte.
        Assert.That(alerts.Select(a => (a.AssetClass, a.Kind, a.LimitPercentage)),
            Is.EqualTo(new[] { (AssetClass.Bonds, AlertKind.BelowMinimum, 60m) }));
        policy.Verify(p => p.For(RiskProfile.Prudent), Times.Once);
    }

    [Test]
    public void Policy_is_not_consulted_for_an_empty_portfolio()
    {
        var policy = new Mock<IAllocationPolicy>(MockBehavior.Strict);
        var evaluator = new AllocationAlertEvaluator(policy.Object);

        Assert.That(evaluator.Evaluate(RiskProfile.Dynamic, Allocation()), Is.Empty);
        policy.VerifyNoOtherCalls();
    }
}
