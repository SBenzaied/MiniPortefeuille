using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;

namespace Portfolio.IntegrationTests;

// Copies locales des contrats JSON : les tests vérifient le JSON réellement exposé,
// sans dépendre des types internes de l'API.

public sealed record ClientSummary(int Id, string Name, RiskProfile RiskProfile, int PositionCount, decimal TotalValueChf, int AlertCount);

public sealed record Position(int Id, string Instrument, AssetClass AssetClass, decimal Quantity, decimal UnitPrice, string Currency, decimal MarketValue);

public sealed record ClientDetail(int Id, string Name, RiskProfile RiskProfile, string AdvisorName, List<Position> Positions);

public sealed record Allocation(AssetClass AssetClass, decimal Amount, decimal Percentage);

public sealed record Alert(AssetClass AssetClass, AlertKind Kind, decimal ActualPercentage, decimal LimitPercentage, string Message);

public sealed record Valuation(string ReferenceCurrency, decimal TotalValue, List<Allocation> Allocations, List<Alert> Alerts);

public sealed record ValidationProblem(string Title, int Status, Dictionary<string, string[]> Errors);
