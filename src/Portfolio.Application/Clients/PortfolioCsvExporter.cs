using System.Globalization;
using System.Text;
using Portfolio.Domain.Valuation;

namespace Portfolio.Application.Clients;

/// <summary>Export d'un portefeuille dans un format tabulaire.</summary>
public interface IPortfolioExporter
{
    /// <summary>Type MIME du contenu produit.</summary>
    string ContentType { get; }

    /// <summary>Extension de fichier, sans point.</summary>
    string FileExtension { get; }

    string Export(ClientDetailDto client, ValuationDto valuation);
}

/// <summary>
/// Export CSV d'un portefeuille, pensé pour Excel en Suisse romande : séparateur « ; »,
/// point décimal, UTF-8 avec BOM (géré par l'appelant via l'encodage).
/// </summary>
public sealed class PortfolioCsvExporter : IPortfolioExporter
{
    private const char Separator = ';';

    public string ContentType => "text/csv; charset=utf-8";

    public string FileExtension => "csv";

    public string Export(ClientDetailDto client, ValuationDto valuation)
    {
        var values = valuation.Positions.ToDictionary(p => p.PositionId, p => p.ValueInReferenceCurrency);
        var csv = new StringBuilder();

        AppendRow(csv, "Instrument", "Classe d'actifs", "Quantité", "Prix unitaire", "Devise", "Valeur (devise)", $"Valeur ({valuation.ReferenceCurrency})");
        foreach (var p in client.Positions)
        {
            AppendRow(csv,
                p.Instrument,
                p.AssetClass.ToString(),
                Number(p.Quantity),
                Number(p.UnitPrice),
                p.Currency,
                Number(p.MarketValue),
                Number(values.GetValueOrDefault(p.Id)));
        }
        AppendRow(csv, "Total", "", "", "", "", "", Number(valuation.TotalValue));
        return csv.ToString();
    }

    private static string Number(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static void AppendRow(StringBuilder csv, params string[] cells)
    {
        csv.AppendJoin(Separator, cells.Select(Escape));
        csv.Append("\r\n");
    }

    /// <summary>
    /// Échappe une cellule. Une valeur commençant par = + - @ serait interprétée comme une formule
    /// par un tableur (injection CSV) : elle est préfixée d'une apostrophe.
    /// </summary>
    public static string Escape(string value)
    {
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]) && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            value = "'" + value;
        }
        return value.IndexOfAny([Separator, '"', '\n', '\r']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
