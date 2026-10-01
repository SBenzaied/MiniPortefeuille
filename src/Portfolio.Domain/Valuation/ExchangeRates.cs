using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Valuation;

/// <summary>
/// Table de taux fixes exprimés en CHF. Toute conversion passe par le CHF :
/// montant × taux(source→CHF) ÷ taux(cible→CHF). Aucun arrondi n'est appliqué ici.
/// </summary>
public sealed class ExchangeRates
{
    private readonly Dictionary<string, decimal> _rateToChf;

    public ExchangeRates(IEnumerable<KeyValuePair<string, decimal>> ratesToChf)
    {
        _rateToChf = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, rate) in ratesToChf)
        {
            if (rate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ratesToChf), $"Le taux de {code} doit être strictement positif.");
            }
            _rateToChf[code] = rate;
        }
    }

    public static ExchangeRates From(IEnumerable<Currency> currencies) =>
        new(currencies.Select(c => KeyValuePair.Create(c.Code, c.RateToChf)));

    public IReadOnlyCollection<string> Codes => _rateToChf.Keys;

    public bool IsKnown(string currencyCode) => _rateToChf.ContainsKey(currencyCode);

    public decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }
        return amount * RateOf(fromCurrency) / RateOf(toCurrency);
    }

    private decimal RateOf(string code) =>
        _rateToChf.TryGetValue(code, out var rate)
            ? rate
            : throw new UnknownCurrencyException(code);
}

public sealed class UnknownCurrencyException(string currencyCode)
    : Exception($"Devise inconnue : {currencyCode}.")
{
    public string CurrencyCode { get; } = currencyCode;
}
