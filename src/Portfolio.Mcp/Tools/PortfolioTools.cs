using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Portfolio.Mcp.Api;

namespace Portfolio.Mcp.Tools;

// --- Réponses des outils (sérialisées en JSON pour le modèle) ------------------------------

public sealed record ClientResume(int Id, string Nom, string Profil, int NombrePositions, decimal ValeurChf, IReadOnlyList<string> ClassesHorsProfil);

public sealed record PositionResume(int Id, string Instrument, string Classe, decimal Quantite, decimal PrixUnitaire, string Devise, decimal Valeur);

public sealed record FicheClient(int Id, string Nom, string Profil, string Conseiller, IReadOnlyList<PositionResume> Positions);

public sealed record LigneRepartition(string Classe, decimal Montant, decimal PartPourcent, decimal? MinimumPourcent, decimal? MaximumPourcent, bool HorsProfil);

public sealed record Valorisation(int IdClient, string Devise, decimal ValeurTotale, IReadOnlyList<LigneRepartition> Repartition, IReadOnlyList<string> Alertes, string Remarque);

public sealed record ClientHorsProfil(int Id, string Nom, string Profil, decimal ValeurChf, IReadOnlyList<string> Alertes);

public sealed record Devise(string Code, string Nom, decimal TauxVersChf);

/// <summary>
/// Outils MCP en lecture seule sur les portefeuilles du conseiller configuré. Instanciés par le
/// conteneur à chaque appel : <see cref="IPortfolioApiClient"/> est injecté par le constructeur.
/// </summary>
[McpServerToolType]
public sealed class PortfolioTools(IPortfolioApiClient api)
{
    private const string FictitiousData = "Données fictives, taux de change fixes (exercice de démonstration).";

    [McpServerTool(Name = "lister_clients", Title = "Lister les clients", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Liste les clients du conseiller : profil de risque, nombre de positions, valeur totale en CHF et classes d'actifs hors profil. Point de départ pour retrouver l'identifiant d'un client.")]
    public async Task<IReadOnlyList<ClientResume>> ListerClients(
        [Description("Filtre facultatif par profil de risque : Prudent, Équilibré ou Dynamique.")] string? profil = null,
        CancellationToken ct = default)
    {
        string? apiProfile = null;
        if (!string.IsNullOrWhiteSpace(profil))
        {
            apiProfile = Labels.ParseRiskProfile(profil)
                ?? throw new McpException($"Profil inconnu : « {profil} ». Valeurs possibles : Prudent, Équilibré, Dynamique.");
        }

        var clients = await api.ListClientsAsync(apiProfile, ct);
        return clients.Select(c => new ClientResume(
            c.Id, c.Name, Labels.RiskProfile(c.RiskProfile), c.PositionCount, c.TotalValueChf,
            c.AlertAssetClasses.Select(Labels.AssetClass).ToList())).ToList();
    }

    [McpServerTool(Name = "consulter_client", Title = "Consulter un client", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Fiche d'un client : profil, conseiller et détail des positions (instrument, classe d'actifs, quantité, prix unitaire, devise, valeur dans la devise de la position).")]
    public async Task<FicheClient> ConsulterClient(
        [Description("Identifiant du client, obtenu avec lister_clients.")] int idClient,
        CancellationToken ct = default)
    {
        var client = await api.GetClientAsync(idClient, ct) ?? throw ClientNotFound(idClient);
        return new FicheClient(client.Id, client.Name, Labels.RiskProfile(client.RiskProfile), client.AdvisorName,
            client.Positions.Select(p => new PositionResume(
                p.Id, p.Instrument, Labels.AssetClass(p.AssetClass), p.Quantity, p.UnitPrice, p.Currency, p.MarketValue)).ToList());
    }

    [McpServerTool(Name = "valoriser_portefeuille", Title = "Valoriser un portefeuille", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Valeur totale d'un portefeuille dans une devise de référence, répartition par classe d'actifs (montant et part en %) comparée aux bornes du profil de risque, et alertes si la répartition sort du profil.")]
    public async Task<Valorisation> ValoriserPortefeuille(
        [Description("Identifiant du client, obtenu avec lister_clients.")] int idClient,
        [Description("Devise de référence (code ISO, par défaut CHF). Voir lister_devises.")] string devise = "CHF",
        CancellationToken ct = default)
    {
        Valuation? valuation;
        try
        {
            valuation = await api.GetValuationAsync(idClient, devise, ct);
        }
        catch (PortfolioApiException ex)
        {
            throw new McpException(ex.Message);
        }
        if (valuation is null)
        {
            throw ClientNotFound(idClient);
        }

        var breached = valuation.Alerts.Select(a => a.AssetClass).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lines = valuation.Allocations.Select(a =>
        {
            var limit = valuation.Limits.FirstOrDefault(l => string.Equals(l.AssetClass, a.AssetClass, StringComparison.OrdinalIgnoreCase));
            return new LigneRepartition(Labels.AssetClass(a.AssetClass), a.Amount, a.Percentage,
                limit?.MinPercentage, limit?.MaxPercentage, breached.Contains(a.AssetClass));
        }).ToList();

        return new Valorisation(idClient, valuation.ReferenceCurrency, valuation.TotalValue, lines,
            valuation.Alerts.Select(a => a.Message).ToList(), FictitiousData);
    }

    [McpServerTool(Name = "clients_hors_profil", Title = "Clients hors profil", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Clients dont la répartition sort des bornes de leur profil de risque, avec le détail de chaque écart. Utile pour préparer des rendez-vous ou un rééquilibrage.")]
    public async Task<IReadOnlyList<ClientHorsProfil>> ClientsHorsProfil(CancellationToken ct = default)
    {
        var flagged = (await api.ListClientsAsync(null, ct)).Where(c => c.AlertCount > 0).ToList();
        var results = new List<ClientHorsProfil>();
        foreach (var client in flagged)
        {
            var valuation = await api.GetValuationAsync(client.Id, "CHF", ct);
            results.Add(new ClientHorsProfil(client.Id, client.Name, Labels.RiskProfile(client.RiskProfile), client.TotalValueChf,
                valuation?.Alerts.Select(a => a.Message).ToList() ?? []));
        }
        return results;
    }

    [McpServerTool(Name = "lister_devises", Title = "Lister les devises", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Devises disponibles comme devise de référence, avec leur taux fixe vers le franc suisse (1 unité = taux CHF).")]
    public async Task<IReadOnlyList<Devise>> ListerDevises(CancellationToken ct = default) =>
        (await api.ListCurrenciesAsync(ct)).Select(c => new Devise(c.Code, c.Name, c.RateToChf)).ToList();

    // Même message que le client existe chez un autre conseiller ou pas du tout (pas de fuite d'information).
    private static McpException ClientNotFound(int idClient) =>
        new($"Aucun client avec l'identifiant {idClient} dans le portefeuille du conseiller. Utiliser lister_clients pour obtenir les identifiants.");
}
