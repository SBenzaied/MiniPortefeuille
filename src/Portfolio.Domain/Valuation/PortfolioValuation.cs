using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Valuation;

/// <summary>Valorisation d'un portefeuille dans une devise de référence.</summary>
/// <param name="Allocations">Une ligne par classe d'actifs, toujours dans l'ordre de l'énumération, y compris à zéro.</param>
public sealed record PortfolioValuation(
    string ReferenceCurrency,
    decimal TotalValue,
    IReadOnlyList<AssetClassAllocation> Allocations,
    IReadOnlyList<PositionValuation> Positions)
{
    public AssetClassAllocation For(AssetClass assetClass) =>
        Allocations.Single(a => a.AssetClass == assetClass);
}

/// <param name="Percentage">Part du total en pourcent (0 à 100), arrondie à 2 décimales.</param>
public sealed record AssetClassAllocation(AssetClass AssetClass, decimal Amount, decimal Percentage);

/// <param name="ValueInReferenceCurrency">Valeur convertie, arrondie à 2 décimales.</param>
public sealed record PositionValuation(int PositionId, decimal ValueInReferenceCurrency);
