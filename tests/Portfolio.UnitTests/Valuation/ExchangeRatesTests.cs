using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Valuation;

[TestFixture]
public class ExchangeRatesTests
{
    private static readonly ExchangeRates Rates = new(new Dictionary<string, decimal>
    {
        ["CHF"] = 1m,
        ["EUR"] = 0.95m,
        ["USD"] = 0.80m,
    });

    [TestCase(100, "USD", "CHF", 80)]
    [TestCase(80, "CHF", "USD", 100)]
    [TestCase(95, "USD", "EUR", 80)] // 95 USD = 76 CHF = 80 EUR
    [TestCase(42, "EUR", "EUR", 42)]
    public void Converts_through_chf(decimal amount, string from, string to, decimal expected)
    {
        Assert.That(Rates.Convert(amount, from, to), Is.EqualTo(expected));
    }

    [Test]
    public void Currency_codes_are_case_insensitive()
    {
        Assert.That(Rates.IsKnown("usd"), Is.True);
        Assert.That(Rates.Convert(100, "usd", "chf"), Is.EqualTo(80m));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Non_positive_rates_are_rejected(decimal rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRates(new Dictionary<string, decimal> { ["XXX"] = rate }));
    }
}
