namespace Portfolio.Domain.Entities;

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
}

/// <summary>
/// Trace d'une modification de position : qui, quand, quoi, avant et après.
/// Pas de clé étrangère vers la position : l'entrée doit survivre à sa suppression.
/// </summary>
public class PositionAuditEntry
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public int AdvisorId { get; set; }
    public int ClientId { get; set; }
    public int PositionId { get; set; }
    public AuditAction Action { get; set; }

    /// <summary>Instantané lisible de la position avant l'opération (null pour une création).</summary>
    public string? Before { get; set; }

    /// <summary>Instantané lisible de la position après l'opération (null pour une suppression).</summary>
    public string? After { get; set; }
}
