using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Auth;
using Portfolio.Application.Clients;
using Portfolio.Application.Currencies;
using Portfolio.Application.Positions;
using Portfolio.Domain.Alerts;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Valuation;

namespace Portfolio.Application;

/// <summary>
/// Enregistre chaque abstraction avec son implémentation. Les appelants ne dépendent que des
/// interfaces : remplacer une implémentation (politique d'allocation paramétrable, autre format
/// d'export…) se fait ici, sans toucher aux classes qui l'utilisent.
/// </summary>
/// <remarks>
/// Durées de vie :
/// <list type="bullet">
/// <item><b>Singleton</b> pour les composants sans état (calcul, règles, validation, export) :
/// une seule instance partagée suffit.</item>
/// <item><b>Scoped</b> (une instance par requête HTTP) pour tout ce qui dépend du DbContext ou du
/// conseiller connecté, eux-mêmes propres à la requête.</item>
/// </list>
/// Le conteneur refuse en développement qu'un singleton dépende d'un service scoped
/// (validation des portées), ce qui protège de ce piège classique.
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Infrastructure technique fournie par .NET, injectée elle aussi pour rester remplaçable en test.
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<Advisor>, PasswordHasher<Advisor>>();

        // Règles métier du domaine, sans état.
        services.AddSingleton<IPortfolioValuator, PortfolioValuator>();
        services.AddSingleton<IAllocationPolicy, FixedAllocationPolicy>();
        services.AddSingleton<IAllocationAlertEvaluator, AllocationAlertEvaluator>();

        // Composants applicatifs sans état.
        services.AddSingleton<IPositionValidator, PositionValidator>();
        services.AddSingleton<IPortfolioExporter, PortfolioCsvExporter>();

        // Cas d'usage : dépendent de la base et du conseiller connecté, donc une instance par requête.
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        return services;
    }
}
