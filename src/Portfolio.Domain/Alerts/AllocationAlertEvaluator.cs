using System.Globalization;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.Domain.Alerts;

public enum AlertKind
{
    AboveMaximum,
    BelowMinimum,
}

public sealed record AllocationAlert(
    AssetClass AssetClass,
    AlertKind Kind,
    decimal ActualPercentage,
    decimal LimitPercentage,
    string Message);

/// <summary>
/// Compare la répartition d'un portefeuille aux bornes de son profil de risque.
/// </summary>
/// <remarks>
/// La comparaison porte sur le pourcentage arrondi affiché au conseiller (2 décimales) :
/// un portefeuille affiché à 30.00 % d'actions n'est pas en alerte pour une borne de 30 %.
/// Un portefeuille vide ne déclenche aucune alerte.
/// </remarks>
public static class AllocationAlertEvaluator
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-CH");

    public static IReadOnlyList<AllocationAlert> Evaluate(RiskProfile profile, PortfolioValuation valuation)
    {
        ArgumentNullException.ThrowIfNull(valuation);
        if (valuation.TotalValue == 0m)
        {
            return [];
        }

        var alerts = new List<AllocationAlert>();
        foreach (var limit in AllocationPolicy.For(profile))
        {
            var actual = valuation.For(limit.AssetClass).Percentage;

            if (limit.MaxPercentage is { } max && actual > max)
            {
                alerts.Add(new AllocationAlert(limit.AssetClass, AlertKind.AboveMaximum, actual, max,
                    $"{Label(limit.AssetClass)} : {Format(actual)} du portefeuille, au-dessus du maximum de {Format(max)} pour un profil {Label(profile)}."));
            }
            else if (limit.MinPercentage is { } min && actual < min)
            {
                alerts.Add(new AllocationAlert(limit.AssetClass, AlertKind.BelowMinimum, actual, min,
                    $"{Label(limit.AssetClass)} : {Format(actual)} du portefeuille, en dessous du minimum de {Format(min)} pour un profil {Label(profile)}."));
            }
        }
        return alerts;
    }

    private static string Format(decimal percentage) => percentage.ToString("0.##", French) + " %";

    private static string Label(AssetClass assetClass) => assetClass switch
    {
        AssetClass.Equities => "Actions",
        AssetClass.Bonds => "Obligations",
        AssetClass.Cash => "Liquidités",
        AssetClass.Other => "Autres actifs",
        _ => assetClass.ToString(),
    };

    private static string Label(RiskProfile profile) => profile switch
    {
        RiskProfile.Prudent => "prudent",
        RiskProfile.Balanced => "équilibré",
        RiskProfile.Dynamic => "dynamique",
        _ => profile.ToString(),
    };
}
