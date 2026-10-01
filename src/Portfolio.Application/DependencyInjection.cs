using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Auth;
using Portfolio.Application.Clients;
using Portfolio.Application.Positions;
using Portfolio.Domain.Entities;

namespace Portfolio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<Advisor>, PasswordHasher<Advisor>>();
        services.AddScoped<AuthService>();
        services.AddScoped<ClientService>();
        services.AddScoped<PositionService>();
        return services;
    }
}
