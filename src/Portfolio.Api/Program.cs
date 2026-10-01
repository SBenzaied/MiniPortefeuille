using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Portfolio.Api.Auth;
using Portfolio.Api.Http;
using Portfolio.Application;
using Portfolio.Application.Abstractions;
using Portfolio.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Vérifie au démarrage que chaque service peut être construit et qu'aucun singleton ne capture
// un service scoped : une erreur d'enregistrement échoue tout de suite, pas à la première requête.
builder.Host.UseDefaultServiceProvider(o =>
{
    o.ValidateScopes = true;
    o.ValidateOnBuild = true;
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ValidationResponses.FromInvalidModelState);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// --- Authentification JWT ---------------------------------------------------------------
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .PostConfigure<ILoggerFactory>((jwt, loggers) =>
        jwt.ResolveKey(builder.Environment.IsProduction(), loggers.CreateLogger<JwtOptions>()));
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

// Conseiller connecté : implémentation HTTP du port ICurrentAdvisor de la couche application.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentAdvisor, HttpCurrentAdvisor>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearer.MapInboundClaims = false; // garder "sub" tel quel plutôt que le type de claim WS-* .NET
        bearer.TokenValidationParameters = new()
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.ResolvedKey,
            ValidAlgorithms = ["HS256"],
            NameClaimType = JwtRegisteredClaimNames.Name,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// --- Limitation des tentatives de connexion ---------------------------------------------
var loginPermitPerMinute = builder.Configuration.GetValue("RateLimiting:LoginPermitPerMinute", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Login, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = loginPermitPerMinute, Window = TimeSpan.FromMinutes(1) }));
});

// --- Derrière un reverse proxy (nginx, Render) -------------------------------------------
// Sans cela, l'API ne voit que l'adresse du proxy et la limite de connexions s'applique à tous
// les utilisateurs ensemble. À n'activer que si l'API n'est joignable QUE par le proxy : on fait
// confiance à son en-tête X-Forwarded-For, en ne lisant que la dernière adresse ajoutée (celle
// qu'il a vue), jamais celles qu'un client aurait pu écrire lui-même.
var trustForwardedHeaders = builder.Configuration.GetValue("ReverseProxy:TrustForwardedHeaders", false);
if (trustForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear(); // adresse du proxy inconnue à l'avance (conteneur, plateforme)
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

if (trustForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler(); // 500 en ProblemDetails, sans détail technique
app.UseStatusCodePages();  // corps ProblemDetails pour 401/403/404 « nus »

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Déploiement en un seul service (Render) : l'image embarque le front dans wwwroot.
var serveFrontend = app.Environment.HasFrontend();
if (serveFrontend)
{
    app.UseFrontend();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");
if (serveFrontend)
{
    app.MapFrontendFallback();
}

await app.Services.InitializeDatabaseAsync(app.Configuration);

await app.RunAsync();

/// <summary>Point d'entrée exposé pour WebApplicationFactory dans les tests d'intégration.</summary>
public partial class Program;
