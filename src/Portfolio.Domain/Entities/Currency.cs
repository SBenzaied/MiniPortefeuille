namespace Portfolio.Domain.Entities;

/// <summary>
/// Devise connue de l'application, avec un taux de change fixe vers le franc suisse
/// (1 unité de la devise = <see cref="RateToChf"/> CHF).
/// </summary>
public class Currency
{
    public const string Chf = "CHF";

    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal RateToChf { get; set; }
}
