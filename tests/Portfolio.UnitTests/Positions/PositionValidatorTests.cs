using System.Globalization;
using Portfolio.Application.Positions;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Positions;

[TestFixture]
public class PositionValidatorTests
{
    private static readonly IPositionValidator Validator = new PositionValidator();

    private static readonly string[] Currencies = ["CHF", "EUR", "USD"];

    private static PositionRequest Valid() => new("Nestlé SA", "Equities", 10m, 88.5m, "CHF");

    private static IReadOnlyDictionary<string, string[]> ErrorsFor(PositionRequest request) =>
        Validator.Validate(request, Currencies).Errors;

    [Test]
    public void Valid_request_is_accepted_and_normalized()
    {
        var (value, errors) = Validator.Validate(new PositionRequest("  Nestlé SA ", "equities", 10m, 88.5m, " eur"), Currencies);

        Assert.That(errors, Is.Empty);
        Assert.That(value, Is.EqualTo(new ValidPosition("Nestlé SA", AssetClass.Equities, 10m, 88.5m, "EUR")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Instrument_is_required(string? instrument)
    {
        Assert.That(ErrorsFor(Valid() with { Instrument = instrument }), Does.ContainKey("instrument"));
    }

    [Test]
    public void Instrument_is_limited_to_100_characters()
    {
        Assert.That(ErrorsFor(Valid() with { Instrument = new string('x', 100) }), Is.Empty);
        Assert.That(ErrorsFor(Valid() with { Instrument = new string('x', 101) }), Does.ContainKey("instrument"));
    }

    [TestCase(null)]
    [TestCase("Crypto")]
    [TestCase("1")]                // Enum.TryParse l'accepterait
    [TestCase("Equities, Bonds")]  // idem (combinaison de drapeaux)
    public void Asset_class_must_be_one_of_the_known_names(string? assetClass)
    {
        Assert.That(ErrorsFor(Valid() with { AssetClass = assetClass }), Does.ContainKey("assetClass"));
    }

    [TestCase(null)]
    [TestCase(0)]
    [TestCase(-5)]
    public void Quantity_must_be_strictly_positive(int? quantity)
    {
        Assert.That(ErrorsFor(Valid() with { Quantity = quantity }), Does.ContainKey("quantity"));
    }

    [TestCase(null)]
    [TestCase("0")]
    [TestCase("-0.01")]
    public void Unit_price_must_be_strictly_positive(string? price)
    {
        var unitPrice = price is null ? (decimal?)null : decimal.Parse(price, CultureInfo.InvariantCulture);

        Assert.That(ErrorsFor(Valid() with { UnitPrice = unitPrice }), Does.ContainKey("unitPrice"));
    }

    [Test]
    public void Amounts_accept_at_most_six_decimals()
    {
        Assert.That(ErrorsFor(Valid() with { Quantity = 0.123456m }), Is.Empty);
        Assert.That(ErrorsFor(Valid() with { Quantity = 1.500000000m }), Is.Empty); // zéros non significatifs
        Assert.That(ErrorsFor(Valid() with { Quantity = 0.1234567m }), Does.ContainKey("quantity"));
    }

    [Test]
    public void Amounts_above_the_database_precision_are_rejected()
    {
        Assert.That(ErrorsFor(Valid() with { UnitPrice = PositionValidator.MaxAmount + 1 }), Does.ContainKey("unitPrice"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("JPY")]
    [TestCase("EURO")]
    public void Currency_must_be_known(string? currency)
    {
        Assert.That(ErrorsFor(Valid() with { Currency = currency }), Does.ContainKey("currency"));
    }

    [Test]
    public void All_errors_are_reported_at_once()
    {
        var errors = ErrorsFor(new PositionRequest("", "Crypto", -1, 0, "JPY"));

        Assert.That(errors.Keys, Is.EquivalentTo(new[] { "assetClass", "currency", "instrument", "quantity", "unitPrice" }));
    }

    [Test]
    public void Missing_body_is_reported()
    {
        var (value, errors) = Validator.Validate(null, Currencies);

        Assert.That(value, Is.Null);
        Assert.That(errors, Is.Not.Empty);
    }
}
