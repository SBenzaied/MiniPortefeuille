using Portfolio.Application.Clients;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.UnitTests.Clients;

public class PortfolioCsvExporterTests
{
    [Fact]
    public void Export_lists_positions_with_reference_value_and_total()
    {
        var client = new ClientDetailDto(1, "Jean Rochat", RiskProfile.Prudent, "Alice Martin",
        [
            new PositionDto(10, "Apple Inc.", AssetClass.Equities, 120m, 215m, "USD", 25_800m),
            new PositionDto(11, "Compte courant", AssetClass.Cash, 1m, 10_000.5m, "CHF", 10_000.5m),
        ]);
        var valuation = new ValuationDto("CHF", 30_640.5m, [],
            [new PositionValuation(10, 20_640m), new PositionValuation(11, 10_000.5m)], []);

        var lines = PortfolioCsvExporter.Export(client, valuation).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Instrument;Classe d'actifs;Quantité;Prix unitaire;Devise;Valeur (devise);Valeur (CHF)", lines[0]);
        Assert.Equal("Apple Inc.;Equities;120;215;USD;25800;20640", lines[1]);
        Assert.Equal("Compte courant;Cash;1;10000.5;CHF;10000.5;10000.5", lines[2]);
        Assert.Equal("Total;;;;;;30640.5", lines[3]);
    }

    [Theory]
    [InlineData("Nestlé SA", "Nestlé SA")]
    [InlineData("A;B", "\"A;B\"")]
    [InlineData("Le \"bon\" titre", "\"Le \"\"bon\"\" titre\"")]
    [InlineData("=HYPERLINK(\"http://x\")", "\"'=HYPERLINK(\"\"http://x\"\")\"")]
    [InlineData("+41 22", "'+41 22")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("-12.5", "-12.5")] // un nombre négatif reste un nombre
    public void Cells_are_escaped_and_protected_against_formula_injection(string value, string expected)
    {
        Assert.Equal(expected, PortfolioCsvExporter.Escape(value));
    }
}
