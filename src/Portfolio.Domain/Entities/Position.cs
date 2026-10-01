namespace Portfolio.Domain.Entities;

public class Position
{
    public int Id { get; set; }

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public required string Instrument { get; set; }
    public AssetClass AssetClass { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Code ISO 4217 de la devise de cotation (CHF, EUR, USD).</summary>
    public required string CurrencyCode { get; set; }

    /// <summary>Valeur de marché dans la devise de la position, sans arrondi.</summary>
    public decimal MarketValue => Quantity * UnitPrice;
}
