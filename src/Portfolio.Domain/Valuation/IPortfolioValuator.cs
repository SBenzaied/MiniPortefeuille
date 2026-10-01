using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Valuation;

/// <summary>Valorisation d'un portefeuille : valeur totale et répartition par classe d'actifs.</summary>
public interface IPortfolioValuator
{
    /// <exception cref="UnknownCurrencyException">Devise de référence ou de position inconnue.</exception>
    PortfolioValuation Value(IEnumerable<Position> positions, ExchangeRates rates, string referenceCurrency = Currency.Chf);
}
