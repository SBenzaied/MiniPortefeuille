using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Alerts;

public class AllocationAlertEvaluatorTests
{
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

    [Fact]
    public void Prudent_profile_with_more_than_30_percent_equities_raises_an_alert()
    {
        var alerts = AllocationAlertEvaluator.Evaluate(RiskProfile.Prudent, Allocation(equities: 45, bonds: 50, cash: 5));

        var alert = Assert.Single(alerts);
        Assert.Equal(AssetClass.Equities, alert.AssetClass);
        Assert.Equal(AlertKind.AboveMaximum, alert.Kind);
        Assert.Equal(45m, alert.ActualPercentage);
        Assert.Equal(30m, alert.LimitPercentage);
        Assert.Equal("Actions : 45 % du portefeuille, au-dessus du maximum de 30 % pour un profil prudent.", alert.Message);
    }

    [Fact]
    public void Limit_itself_is_allowed()
    {
        var alerts = AllocationAlertEvaluator.Evaluate(RiskProfile.Prudent, Allocation(equities: 30, bonds: 70));

        Assert.Empty(alerts);
    }

    [Fact]
    public void Comparison_uses_the_rounded_percentage_shown_to_the_advisor()
    {
        // 30.004 % est affiché 30.00 % par le calcul de valorisation : pas d'alerte.
        // On passe par le vrai calcul pour vérifier l'enchaînement valorisation → alerte.
        var rates = new ExchangeRates(new Dictionary<string, decimal> { ["CHF"] = 1m });
        Position P(AssetClass c, decimal value) => new() { Instrument = "x", AssetClass = c, Quantity = 1, UnitPrice = value, CurrencyCode = "CHF" };

        var valuation = PortfolioValuator.Value([P(AssetClass.Equities, 30_004m), P(AssetClass.Bonds, 69_996m)], rates);

        Assert.Equal(30.00m, valuation.For(AssetClass.Equities).Percentage);
        Assert.Empty(AllocationAlertEvaluator.Evaluate(RiskProfile.Prudent, valuation));
    }

    [Fact]
    public void Balanced_profile_with_too_few_equities_raises_a_below_minimum_alert()
    {
        var alerts = AllocationAlertEvaluator.Evaluate(RiskProfile.Balanced, Allocation(equities: 10, bonds: 80, cash: 10));

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertKind.BelowMinimum, alert.Kind);
        Assert.Equal(20m, alert.LimitPercentage);
    }

    [Fact]
    public void Dynamic_profile_mostly_in_cash_raises_two_alerts()
    {
        var alerts = AllocationAlertEvaluator.Evaluate(RiskProfile.Dynamic, Allocation(equities: 20, cash: 80));

        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, a => a is { AssetClass: AssetClass.Equities, Kind: AlertKind.BelowMinimum });
        Assert.Contains(alerts, a => a is { AssetClass: AssetClass.Cash, Kind: AlertKind.AboveMaximum });
    }

    [Theory]
    [InlineData(RiskProfile.Prudent, 20, 70, 10, 0)]
    [InlineData(RiskProfile.Balanced, 45, 35, 10, 10)]
    [InlineData(RiskProfile.Dynamic, 85, 0, 5, 10)]
    public void Portfolio_in_line_with_its_profile_raises_no_alert(RiskProfile profile, decimal equities, decimal bonds, decimal cash, decimal other)
    {
        Assert.Empty(AllocationAlertEvaluator.Evaluate(profile, Allocation(equities, bonds, cash, other)));
    }

    [Theory]
    [InlineData(RiskProfile.Prudent)]
    [InlineData(RiskProfile.Balanced)]
    [InlineData(RiskProfile.Dynamic)]
    public void Empty_portfolio_raises_no_alert(RiskProfile profile)
    {
        // Sinon un portefeuille vide déclencherait « actions sous le minimum » pour un profil dynamique.
        Assert.Empty(AllocationAlertEvaluator.Evaluate(profile, Allocation()));
    }

    [Fact]
    public void Every_profile_has_a_policy()
    {
        Assert.All(Enum.GetValues<RiskProfile>(), p => Assert.NotEmpty(AllocationPolicy.For(p)));
    }
}
