using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Valuation;

public class ExchangeRatesTests
{
    private static readonly ExchangeRates Rates = new(new Dictionary<string, decimal>
    {
        ["CHF"] = 1m,
        ["EUR"] = 0.95m,
        ["USD"] = 0.80m,
    });

    [Theory]
    [InlineData(100, "USD", "CHF", 80)]
    [InlineData(80, "CHF", "USD", 100)]
    [InlineData(95, "USD", "EUR", 80)] // 95 USD = 76 CHF = 80 EUR
    [InlineData(42, "EUR", "EUR", 42)]
    public void Converts_through_chf(decimal amount, string from, string to, decimal expected)
    {
        Assert.Equal(expected, Rates.Convert(amount, from, to));
    }

    [Fact]
    public void Currency_codes_are_case_insensitive()
    {
        Assert.True(Rates.IsKnown("usd"));
        Assert.Equal(80m, Rates.Convert(100, "usd", "chf"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_rates_are_rejected(decimal rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRates(new Dictionary<string, decimal> { ["XXX"] = rate }));
    }
}
