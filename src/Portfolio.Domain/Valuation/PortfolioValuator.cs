using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Valuation;

/// <summary>
/// Calcule la valeur totale d'un portefeuille et sa répartition par classe d'actifs.
/// Fonction pure : aucune dépendance à la base ni à l'horloge, donc testable isolément.
/// </summary>
/// <remarks>
/// Les calculs se font en pleine précision <see cref="decimal"/> ; l'arrondi (2 décimales,
/// au plus proche, 0.5 vers l'extérieur) n'intervient qu'au moment de produire le résultat.
/// Conséquence assumée : la somme des pourcentages arrondis peut différer de 100 de ±0.01 par ligne.
/// </remarks>
public static class PortfolioValuator
{
    public const int AmountDecimals = 2;
    public const int PercentageDecimals = 2;

    public static PortfolioValuation Value(
        IEnumerable<Position> positions,
        ExchangeRates rates,
        string referenceCurrency = Currency.Chf)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(rates);
        if (!rates.IsKnown(referenceCurrency))
        {
            throw new UnknownCurrencyException(referenceCurrency);
        }

        var converted = positions
            .Select(p => (Position: p, Value: rates.Convert(p.MarketValue, p.CurrencyCode, referenceCurrency)))
            .ToList();

        var total = converted.Sum(x => x.Value);

        var allocations = Enum.GetValues<AssetClass>()
            .Select(assetClass =>
            {
                var amount = converted.Where(x => x.Position.AssetClass == assetClass).Sum(x => x.Value);
                var percentage = total == 0m ? 0m : amount / total * 100m;
                return new AssetClassAllocation(assetClass, Round(amount, AmountDecimals), Round(percentage, PercentageDecimals));
            })
            .ToList();

        var positionValues = converted
            .Select(x => new PositionValuation(x.Position.Id, Round(x.Value, AmountDecimals)))
            .ToList();

        return new PortfolioValuation(referenceCurrency.ToUpperInvariant(), Round(total, AmountDecimals), allocations, positionValues);
    }

    private static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
