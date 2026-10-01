namespace Portfolio.Mcp.Api;

// Copies des contrats JSON de l'API REST. Le serveur MCP est un client comme le front : il ne
// référence pas les projets de l'API, seulement le JSON qu'elle expose.

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record ClientSummary(
    int Id, string Name, string RiskProfile, int PositionCount, decimal TotalValueChf, int AlertCount, List<string> AlertAssetClasses);

public sealed record PositionDto(
    int Id, string Instrument, string AssetClass, decimal Quantity, decimal UnitPrice, string Currency, decimal MarketValue);

public sealed record ClientDetail(int Id, string Name, string RiskProfile, string AdvisorName, List<PositionDto> Positions);

public sealed record Allocation(string AssetClass, decimal Amount, decimal Percentage);

public sealed record AllocationAlert(string AssetClass, string Kind, decimal ActualPercentage, decimal LimitPercentage, string Message);

public sealed record AllocationLimit(string AssetClass, decimal? MinPercentage, decimal? MaxPercentage);

public sealed record PositionValue(int PositionId, decimal ValueInReferenceCurrency);

public sealed record Valuation(
    string ReferenceCurrency, decimal TotalValue, List<Allocation> Allocations, List<PositionValue> Positions,
    List<AllocationAlert> Alerts, List<AllocationLimit> Limits);

public sealed record CurrencyDto(string Code, string Name, decimal RateToChf);
