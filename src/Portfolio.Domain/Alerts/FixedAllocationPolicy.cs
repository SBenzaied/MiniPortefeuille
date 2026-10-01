using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Alerts;

/// <summary>
/// Bornes de répartition par profil de risque, codées en dur. Les valeurs sont volontairement
/// simples et fictives : dans un vrai contexte, elles viendraient de la politique
/// d'investissement de la banque et seraient paramétrables (autre implémentation de
/// <see cref="IAllocationPolicy"/>).
/// </summary>
public sealed class FixedAllocationPolicy : IAllocationPolicy
{
    private static readonly Dictionary<RiskProfile, AllocationLimit[]> Limits = new()
    {
        [RiskProfile.Prudent] =
        [
            new(AssetClass.Equities, MinPercentage: null, MaxPercentage: 30m),
            new(AssetClass.Other, MinPercentage: null, MaxPercentage: 10m),
        ],
        [RiskProfile.Balanced] =
        [
            new(AssetClass.Equities, MinPercentage: 20m, MaxPercentage: 60m),
            new(AssetClass.Other, MinPercentage: null, MaxPercentage: 20m),
        ],
        [RiskProfile.Dynamic] =
        [
            new(AssetClass.Equities, MinPercentage: 50m, MaxPercentage: null),
            new(AssetClass.Cash, MinPercentage: null, MaxPercentage: 25m),
        ],
    };

    public IReadOnlyList<AllocationLimit> For(RiskProfile profile) => Limits[profile];
}
