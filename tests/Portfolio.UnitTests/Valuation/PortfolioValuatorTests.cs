using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Valuation;

public class PortfolioValuatorTests
{
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

    [Fact]
    public void Total_is_the_sum_of_quantity_times_price_in_chf_by_default()
    {
        var valuation = PortfolioValuator.Value(
            [P(AssetClass.Equities, 10, 100), P(AssetClass.Bonds, 5, 200), P(AssetClass.Cash, 1, 500)],
            Rates);

        Assert.Equal("CHF", valuation.ReferenceCurrency);
        Assert.Equal(2_500m, valuation.TotalValue);
    }

    [Fact]
    public void Positions_in_foreign_currencies_are_converted_through_chf()
    {
        // 1 000 USD × 0.80 = 800 CHF ; 1 000 EUR × 0.95 = 950 CHF
        var valuation = PortfolioValuator.Value(
            [P(AssetClass.Equities, 10, 100, "USD"), P(AssetClass.Bonds, 10, 100, "EUR")],
            Rates);

        Assert.Equal(1_750m, valuation.TotalValue);
        Assert.Equal(800m, valuation.For(AssetClass.Equities).Amount);
        Assert.Equal(950m, valuation.For(AssetClass.Bonds).Amount);
    }

    [Fact]
    public void Valuation_can_be_expressed_in_another_reference_currency()
    {
        // 1 000 USD = 800 CHF = 800 / 0.95 EUR = 842.105… → 842.11 EUR
        var valuation = PortfolioValuator.Value([P(AssetClass.Equities, 1, 1_000, "USD")], Rates, "EUR");

        Assert.Equal("EUR", valuation.ReferenceCurrency);
        Assert.Equal(842.11m, valuation.TotalValue);
    }

    [Fact]
    public void A_position_already_in_the_reference_currency_is_not_converted()
    {
        var valuation = PortfolioValuator.Value([P(AssetClass.Cash, 1, 1_234.56m, "USD")], Rates, "USD");

        Assert.Equal(1_234.56m, valuation.TotalValue);
    }

    [Fact]
    public void Allocation_gives_amount_and_percentage_per_asset_class()
    {
        var valuation = PortfolioValuator.Value(
            [
                P(AssetClass.Equities, 1, 600),
                P(AssetClass.Equities, 1, 150),
                P(AssetClass.Bonds, 1, 200),
                P(AssetClass.Cash, 1, 50),
            ],
            Rates);

        Assert.Equal(1_000m, valuation.TotalValue);
        Assert.Equal((750m, 75m), (valuation.For(AssetClass.Equities).Amount, valuation.For(AssetClass.Equities).Percentage));
        Assert.Equal((200m, 20m), (valuation.For(AssetClass.Bonds).Amount, valuation.For(AssetClass.Bonds).Percentage));
        Assert.Equal((50m, 5m), (valuation.For(AssetClass.Cash).Amount, valuation.For(AssetClass.Cash).Percentage));
    }

    [Fact]
    public void Every_asset_class_is_listed_even_when_absent()
    {
        var valuation = PortfolioValuator.Value([P(AssetClass.Bonds, 1, 100)], Rates);

        Assert.Equal(Enum.GetValues<AssetClass>(), valuation.Allocations.Select(a => a.AssetClass));
        Assert.Equal(0m, valuation.For(AssetClass.Other).Amount);
        Assert.Equal(0m, valuation.For(AssetClass.Other).Percentage);
        Assert.Equal(100m, valuation.For(AssetClass.Bonds).Percentage);
    }

    [Fact]
    public void Empty_portfolio_is_worth_zero_with_zero_percentages()
    {
        var valuation = PortfolioValuator.Value([], Rates);

        Assert.Equal(0m, valuation.TotalValue);
        Assert.All(valuation.Allocations, a => Assert.Equal(0m, a.Percentage));
        Assert.Empty(valuation.Positions);
    }

    [Fact]
    public void Percentages_are_rounded_to_two_decimals_from_unrounded_amounts()
    {
        // Trois tiers : 33.333… % chacun, arrondi à 33.33 ; la somme affichée vaut 99.99.
        var valuation = PortfolioValuator.Value(
            [P(AssetClass.Equities, 1, 100), P(AssetClass.Bonds, 1, 100), P(AssetClass.Cash, 1, 100)],
            Rates);

        Assert.Equal(33.33m, valuation.For(AssetClass.Equities).Percentage);
        Assert.Equal(99.99m, valuation.Allocations.Sum(a => a.Percentage));
    }

    [Fact]
    public void Amounts_are_rounded_half_away_from_zero_only_at_the_end()
    {
        // Deux positions à 0.005 CHF : arrondies une à une elles donneraient 0.02 (ou 0.00 en
        // arrondi bancaire) ; la somme exacte 0.010 donne bien 0.01.
        var valuation = PortfolioValuator.Value(
            [P(AssetClass.Cash, 1, 0.005m), P(AssetClass.Cash, 1, 0.005m)],
            Rates);

        Assert.Equal(0.01m, valuation.TotalValue);
        Assert.All(valuation.Positions, p => Assert.Equal(0.01m, p.ValueInReferenceCurrency)); // 0.005 → 0.01 (AwayFromZero)
    }

    [Fact]
    public void Each_position_value_is_reported_in_the_reference_currency()
    {
        var usd = P(AssetClass.Equities, 3, 10, "USD");
        var valuation = PortfolioValuator.Value([usd], Rates);

        var line = Assert.Single(valuation.Positions);
        Assert.Equal(usd.Id, line.PositionId);
        Assert.Equal(24m, line.ValueInReferenceCurrency);
    }

    [Fact]
    public void Unknown_reference_currency_is_rejected()
    {
        var ex = Assert.Throws<UnknownCurrencyException>(() => PortfolioValuator.Value([], Rates, "JPY"));
        Assert.Equal("JPY", ex.CurrencyCode);
    }

    [Fact]
    public void Position_in_an_unknown_currency_is_rejected()
    {
        Assert.Throws<UnknownCurrencyException>(() => PortfolioValuator.Value([P(AssetClass.Cash, 1, 1, "GBP")], Rates));
    }
}
