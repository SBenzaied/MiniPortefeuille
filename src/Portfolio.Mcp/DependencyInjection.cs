using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Portfolio.Mcp.Api;
using Portfolio.Mcp.Tools;

namespace Portfolio.Mcp;

public static class DependencyInjection
{
    /// <summary>
    /// Client de l'API REST : options validées au démarrage, fournisseur de jeton (singleton),
    /// gestionnaire HTTP qui ajoute le jeton, et client typé exposé par son interface.
    /// </summary>
    public static IServiceCollection AddPortfolioApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PortfolioApiOptions>()
            .Bind(configuration.GetSection(PortfolioApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAdvisorTokenProvider, AdvisorTokenProvider>();
        services.AddTransient<AdvisorAuthenticationHandler>();

        services.AddHttpClient(AdvisorTokenProvider.HttpClientName, ConfigureBaseAddress);
        services.AddHttpClient<IPortfolioApiClient, PortfolioApiClient>(ConfigureBaseAddress)
            .AddHttpMessageHandler<AdvisorAuthenticationHandler>();
        return services;
    }

    /// <summary>Serveur MCP et ses outils ; le transport (stdio ou HTTP) est choisi par l'appelant.</summary>
    public static IMcpServerBuilder AddPortfolioMcpServer(this IServiceCollection services) =>
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "mini-portefeuille",
                    Title = "Mini-portefeuille",
                    Version = typeof(DependencyInjection).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
                };
                options.ServerInstructions =
                    "Portefeuilles des clients d'un conseiller en patrimoine (données fictives, taux de change fixes). " +
                    "Lecture seule. Commencer par lister_clients pour obtenir les identifiants. Montants au format " +
                    "suisse (CHF 48'330.00). Une alerte signale une répartition hors des bornes du profil de risque.";
            })
            .WithTools<PortfolioTools>();

    private static void ConfigureBaseAddress(IServiceProvider services, HttpClient http)
    {
        http.BaseAddress = new Uri(services.GetRequiredService<IOptions<PortfolioApiOptions>>().Value.BaseUrl);
        // Large : sur l'offre gratuite de Render, l'API peut mettre une minute à sortir de veille.
        http.Timeout = TimeSpan.FromSeconds(90);
    }
}
