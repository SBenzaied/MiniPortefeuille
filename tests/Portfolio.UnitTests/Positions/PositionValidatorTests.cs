using Portfolio.Application.Positions;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Positions;

public class PositionValidatorTests
{
    private static readonly string[] Currencies = ["CHF", "EUR", "USD"];

    private static PositionRequest Valid() => new("Nestlé SA", "Equities", 10m, 88.5m, "CHF");

    private static IReadOnlyDictionary<string, string[]> ErrorsFor(PositionRequest request) =>
        PositionValidator.Validate(request, Currencies).Errors;

    [Fact]
    public void Valid_request_is_accepted_and_normalized()
    {
        var (value, errors) = PositionValidator.Validate(new PositionRequest("  Nestlé SA ", "equities", 10m, 88.5m, " eur"), Currencies);

        Assert.Empty(errors);
        Assert.Equal(new ValidPosition("Nestlé SA", AssetClass.Equities, 10m, 88.5m, "EUR"), value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Instrument_is_required(string? instrument)
    {
        Assert.Contains("instrument", ErrorsFor(Valid() with { Instrument = instrument }).Keys);
    }

    [Fact]
    public void Instrument_is_limited_to_100_characters()
    {
        Assert.Empty(ErrorsFor(Valid() with { Instrument = new string('x', 100) }));
        Assert.Contains("instrument", ErrorsFor(Valid() with { Instrument = new string('x', 101) }).Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Crypto")]
    [InlineData("1")]                // Enum.TryParse l'accepterait
    [InlineData("Equities, Bonds")]  // idem (combinaison de drapeaux)
    public void Asset_class_must_be_one_of_the_known_names(string? assetClass)
    {
        Assert.Contains("assetClass", ErrorsFor(Valid() with { AssetClass = assetClass }).Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void Quantity_must_be_strictly_positive(int? quantity)
    {
        var errors = ErrorsFor(Valid() with { Quantity = quantity });

        Assert.Contains("quantity", errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void Unit_price_must_be_strictly_positive(string? price)
    {
        var unitPrice = price is null ? (decimal?)null : decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Contains("unitPrice", ErrorsFor(Valid() with { UnitPrice = unitPrice }).Keys);
    }

    [Fact]
    public void Amounts_accept_at_most_six_decimals()
    {
        Assert.Empty(ErrorsFor(Valid() with { Quantity = 0.123456m }));
        Assert.Empty(ErrorsFor(Valid() with { Quantity = 1.500000000m })); // zéros non significatifs
        Assert.Contains("quantity", ErrorsFor(Valid() with { Quantity = 0.1234567m }).Keys);
    }

    [Fact]
    public void Amounts_above_the_database_precision_are_rejected()
    {
        Assert.Contains("unitPrice", ErrorsFor(Valid() with { UnitPrice = PositionValidator.MaxAmount + 1 }).Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("JPY")]
    [InlineData("EURO")]
    public void Currency_must_be_known(string? currency)
    {
        var errors = ErrorsFor(Valid() with { Currency = currency });

        Assert.Contains("currency", errors.Keys);
    }

    [Fact]
    public void All_errors_are_reported_at_once()
    {
        var errors = ErrorsFor(new PositionRequest("", "Crypto", -1, 0, "JPY"));

        Assert.Equal(["assetClass", "currency", "instrument", "quantity", "unitPrice"], errors.Keys.Order());
    }

    [Fact]
    public void Missing_body_is_reported()
    {
        var (value, errors) = PositionValidator.Validate(null, Currencies);

        Assert.Null(value);
        Assert.NotEmpty(errors);
    }
}
