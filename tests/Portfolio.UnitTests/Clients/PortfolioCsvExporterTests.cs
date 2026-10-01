using Portfolio.Application.Clients;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Clients;

[TestFixture]
public class PortfolioCsvExporterTests
{
    private readonly IPortfolioExporter _exporter = new PortfolioCsvExporter();

    [Test]
    public void Export_lists_positions_with_reference_value_and_total()
    {
        var client = new ClientDetailDto(1, "Jean Rochat", RiskProfile.Prudent, "Alice Martin",
        [
            new PositionDto(10, "Apple Inc.", AssetClass.Equities, 120m, 215m, "USD", 25_800m),
            new PositionDto(11, "Compte courant", AssetClass.Cash, 1m, 10_000.5m, "CHF", 10_000.5m),
        ]);
        var valuation = new ValuationDto("CHF", 30_640.5m, [],
            [new PositionValuation(10, 20_640m), new PositionValuation(11, 10_000.5m)], [], []);

        var lines = _exporter.Export(client, valuation).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.That(lines, Is.EqualTo(new[]
        {
            "Instrument;Classe d'actifs;Quantité;Prix unitaire;Devise;Valeur (devise);Valeur (CHF)",
            "Apple Inc.;Equities;120;215;USD;25800;20640",
            "Compte courant;Cash;1;10000.5;CHF;10000.5;10000.5",
            "Total;;;;;;30640.5",
        }));
    }

    [Test]
    public void Exporter_describes_its_format()
    {
        Assert.That(_exporter.ContentType, Does.StartWith("text/csv"));
        Assert.That(_exporter.FileExtension, Is.EqualTo("csv"));
    }

    [TestCase("Nestlé SA", "Nestlé SA")]
    [TestCase("A;B", "\"A;B\"")]
    [TestCase("Le \"bon\" titre", "\"Le \"\"bon\"\" titre\"")]
    [TestCase("=HYPERLINK(\"http://x\")", "\"'=HYPERLINK(\"\"http://x\"\")\"")]
    [TestCase("+41 22", "'+41 22")]
    [TestCase("@SUM(A1)", "'@SUM(A1)")]
    [TestCase("-12.5", "-12.5")] // un nombre négatif reste un nombre
    public void Cells_are_escaped_and_protected_against_formula_injection(string value, string expected)
    {
        Assert.That(PortfolioCsvExporter.Escape(value), Is.EqualTo(expected));
    }
}
