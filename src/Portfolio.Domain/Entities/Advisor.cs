namespace Portfolio.Domain.Entities;

/// <summary>Conseiller en patrimoine. C'est l'utilisateur qui se connecte à l'application.</summary>
public class Advisor
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string DisplayName { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    public List<Client> Clients { get; set; } = [];
}
