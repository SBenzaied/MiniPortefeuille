using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Valuation;

[TestFixture]
public class PortfolioValuatorTests
{
    private static readonly IPortfolioValuator Valuator = new PortfolioValuator();

    // Taux fictifs volontairement « ronds » pour que les attentes se calculent de tête.
    private static readonly ExchangeRates Rates = new(new Dictionary<string, decimal>
    {
        ["CHF"] = 1m,
        ["EUR"] = 0.95m,
        ["USD"] = 0.80m,
    });

    private static int _nextId;

    private static Position P(AssetClass assetClass, decimal quantity, decimal unitPrice, string currency = "CHF") => new()
    {
        Id = Interlocked.Increment(ref _nextId),
        Instrument = $"Instrument {assetClass}",
        AssetClass = assetClass,
        Quantity = quantity,
        UnitPrice = unitPrice,
        CurrencyCode = currency,
    };

    [Test]
    public void Total_is_the_sum_of_quantity_times_price_in_chf_by_default()
    {
        var valuation = Valuator.Value(
            [P(AssetClass.Equities, 10, 100), P(AssetClass.Bonds, 5, 200), P(AssetClass.Cash, 1, 500)],
            Rates);

        Assert.That(valuation.ReferenceCurrency, Is.EqualTo("CHF"));
        Assert.That(valuation.TotalValue, Is.EqualTo(2_500m));
    }

    [Test]
    public void Positions_in_foreign_currencies_are_converted_through_chf()
    {
        // 1 000 USD × 0.80 = 800 CHF ; 1 000 EUR × 0.95 = 950 CHF
        var valuation = Valuator.Value(
            [P(AssetClass.Equities, 10, 100, "USD"), P(AssetClass.Bonds, 10, 100, "EUR")],
            Rates);

        Assert.That(valuation.TotalValue, Is.EqualTo(1_750m));
        Assert.That(valuation.For(AssetClass.Equities).Amount, Is.EqualTo(800m));
        Assert.That(valuation.For(AssetClass.Bonds).Amount, Is.EqualTo(950m));
    }

    [Test]
    public void Valuation_can_be_expressed_in_another_reference_currency()
    {
        // 1 000 USD = 800 CHF = 800 / 0.95 EUR = 842.105… → 842.11 EUR
        var valuation = Valuator.Value([P(AssetClass.Equities, 1, 1_000, "USD")], Rates, "EUR");

        Assert.That(valuation.ReferenceCurrency, Is.EqualTo("EUR"));
        Assert.That(valuation.TotalValue, Is.EqualTo(842.11m));
    }

    [Test]
    public void A_position_already_in_the_reference_currency_is_not_converted()
    {
        var valuation = Valuator.Value([P(AssetClass.Cash, 1, 1_234.56m, "USD")], Rates, "USD");

        Assert.That(valuation.TotalValue, Is.EqualTo(1_234.56m));
    }

    [Test]
    public void Allocation_gives_amount_and_percentage_per_asset_class()
    {
        var valuation = Valuator.Value(
            [
                P(AssetClass.Equities, 1, 600),
                P(AssetClass.Equities, 1, 150),
                P(AssetClass.Bonds, 1, 200),
                P(AssetClass.Cash, 1, 50),
            ],
            Rates);

        Assert.That(valuation.TotalValue, Is.EqualTo(1_000m));
        Assert.That(valuation.For(AssetClass.Equities), Has.Property("Amount").EqualTo(750m).And.Property("Percentage").EqualTo(75m));
        Assert.That(valuation.For(AssetClass.Bonds), Has.Property("Amount").EqualTo(200m).And.Property("Percentage").EqualTo(20m));
        Assert.That(valuation.For(AssetClass.Cash), Has.Property("Amount").EqualTo(50m).And.Property("Percentage").EqualTo(5m));
    }

    [Test]
    public void Every_asset_class_is_listed_even_when_absent()
    {
        var valuation = Valuator.Value([P(AssetClass.Bonds, 1, 100)], Rates);

        Assert.That(valuation.Allocations.Select(a => a.AssetClass), Is.EqualTo(Enum.GetValues<AssetClass>()));
        Assert.That(valuation.For(AssetClass.Other).Amount, Is.Zero);
        Assert.That(valuation.For(AssetClass.Other).Percentage, Is.Zero);
        Assert.That(valuation.For(AssetClass.Bonds).Percentage, Is.EqualTo(100m));
    }

    [Test]
    public void Empty_portfolio_is_worth_zero_with_zero_percentages()
    {
        var valuation = Valuator.Value([], Rates);

        Assert.That(valuation.TotalValue, Is.Zero);
        Assert.That(valuation.Allocations.Select(a => a.Percentage), Is.All.Zero);
        Assert.That(valuation.Positions, Is.Empty);
    }

    [Test]
    public void Percentages_are_rounded_to_two_decimals_from_unrounded_amounts()
    {
        // Trois tiers : 33.333… % chacun, arrondi à 33.33 ; la somme affichée vaut 99.99.
        var valuation = Valuator.Value(
            [P(AssetClass.Equities, 1, 100), P(AssetClass.Bonds, 1, 100), P(AssetClass.Cash, 1, 100)],
            Rates);

        Assert.That(valuation.For(AssetClass.Equities).Percentage, Is.EqualTo(33.33m));
        Assert.That(valuation.Allocations.Sum(a => a.Percentage), Is.EqualTo(99.99m));
    }

    [Test]
    public void Amounts_are_rounded_half_away_from_zero_only_at_the_end()
    {
        // Deux positions à 0.005 CHF : arrondies une à une elles donneraient 0.02 (ou 0.00 en
        // arrondi bancaire) ; la somme exacte 0.010 donne bien 0.01.
        var valuation = Valuator.Value(
            [P(AssetClass.Cash, 1, 0.005m), P(AssetClass.Cash, 1, 0.005m)],
            Rates);

        Assert.That(valuation.TotalValue, Is.EqualTo(0.01m));
        Assert.That(valuation.Positions.Select(p => p.ValueInReferenceCurrency), Is.All.EqualTo(0.01m)); // 0.005 → 0.01 (AwayFromZero)
    }

    [Test]
    public void Each_position_value_is_reported_in_the_reference_currency()
    {
        var usd = P(AssetClass.Equities, 3, 10, "USD");
        var valuation = Valuator.Value([usd], Rates);

        Assert.That(valuation.Positions, Has.Count.EqualTo(1));
        Assert.That(valuation.Positions[0], Is.EqualTo(new PositionValuation(usd.Id, 24m)));
    }

    [Test]
    public void Unknown_reference_currency_is_rejected()
    {
        var ex = Assert.Throws<UnknownCurrencyException>(() => Valuator.Value([], Rates, "JPY"));
        Assert.That(ex!.CurrencyCode, Is.EqualTo("JPY"));
    }

    [Test]
    public void Position_in_an_unknown_currency_is_rejected()
    {
        Assert.Throws<UnknownCurrencyException>(() => Valuator.Value([P(AssetClass.Cash, 1, 1, "GBP")], Rates));
    }
}
