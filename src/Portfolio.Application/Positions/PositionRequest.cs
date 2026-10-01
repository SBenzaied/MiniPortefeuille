namespace Portfolio.Application.Positions;

/// <summary>
/// Corps de requête pour créer ou modifier une position. Les champs sont volontairement
/// nullables et la classe d'actifs est une chaîne : c'est le validateur, et non la
/// désérialisation JSON, qui produit des messages d'erreur lisibles champ par champ.
/// </summary>
public sealed record PositionRequest(
    string? Instrument,
    string? AssetClass,
    decimal? Quantity,
    decimal? UnitPrice,
    string? Currency);
