using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Alerts;

/// <summary>Borne de répartition pour une classe d'actifs, en pourcent du portefeuille (bornes incluses).</summary>
public sealed record AllocationLimit(AssetClass AssetClass, decimal? MinPercentage, decimal? MaxPercentage);

/// <summary>
/// Bornes de répartition attendues pour chaque profil de risque. L'implémentation actuelle est
/// une table fixe (<see cref="FixedAllocationPolicy"/>) ; une politique d'investissement lue en
/// base ou depuis un service de la banque se brancherait ici sans toucher aux appelants.
/// </summary>
public interface IAllocationPolicy
{
    IReadOnlyList<AllocationLimit> For(RiskProfile profile);
}
