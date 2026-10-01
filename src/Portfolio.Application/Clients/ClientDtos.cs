using Portfolio.Application.Common;
using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.Application.Clients;

/// <param name="AlertAssetClasses">Classes d'actifs hors profil, pour nommer l'écart dès la liste.</param>
public sealed record ClientSummaryDto(
    int Id,
    string Name,
    RiskProfile RiskProfile,
    int PositionCount,
    decimal TotalValueChf,
    int AlertCount,
    IReadOnlyList<AssetClass> AlertAssetClasses);

public sealed record ClientDetailDto(
    int Id,
    string Name,
    RiskProfile RiskProfile,
    string AdvisorName,
    IReadOnlyList<PositionDto> Positions);

/// <param name="MarketValue">Quantité × prix unitaire, dans la devise de la position, arrondie à 2 décimales.</param>
public sealed record PositionDto(
    int Id,
    string Instrument,
    AssetClass AssetClass,
    decimal Quantity,
    decimal UnitPrice,
    string Currency,
    decimal MarketValue)
{
    public static PositionDto From(Position p) => new(
        p.Id, p.Instrument, p.AssetClass, p.Quantity.Normalize(), p.UnitPrice.Normalize(), p.CurrencyCode,
        Math.Round(p.MarketValue, PortfolioValuator.AmountDecimals, MidpointRounding.AwayFromZero));
}

/// <param name="Limits">Bornes du profil de risque du client, pour que le front trace la bande autorisée.</param>
public sealed record ValuationDto(
    string ReferenceCurrency,
    decimal TotalValue,
    IReadOnlyList<AssetClassAllocation> Allocations,
    IReadOnlyList<PositionValuation> Positions,
    IReadOnlyList<AllocationAlert> Alerts,
    IReadOnlyList<AllocationLimit> Limits);
