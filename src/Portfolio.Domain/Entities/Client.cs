namespace Portfolio.Domain.Entities;

public class Client
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public RiskProfile RiskProfile { get; set; }

    public int AdvisorId { get; set; }
    public Advisor? Advisor { get; set; }

    public List<Position> Positions { get; set; } = [];
}
