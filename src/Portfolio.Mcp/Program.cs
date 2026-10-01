using Portfolio.Mcp.Http;

namespace Portfolio.Mcp;

/// <summary>
/// Deux modes, mêmes outils :
/// <list type="bullet">
/// <item><c>Portfolio.Mcp --stdio</c> : Claude Desktop ou Claude Code lancent le serveur en local.</item>
/// <item>sans argument : serveur HTTP (<c>POST /mcp</c>), pour un connecteur distant.</item>
/// </list>
/// </summary>
/// <remarks>
/// Point d'entrée explicite plutôt qu'en instructions de premier niveau : sinon .NET génère une
/// classe <c>Program</c> publique qui entre en conflit avec celle de l'API dans les tests.
/// </remarks>
public static class McpServerProgram
{
    private const string StdioSwitch = "--stdio";

    public static Task Main(string[] args) =>
        args.Contains(StdioSwitch) ? RunStdioAsync(args.Where(a => a != StdioSwitch).ToArray()) : RunHttpAsync(args);

    private static async Task RunStdioAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            // Le client MCP lance le processus depuis n'importe quel dossier : appsettings.json est cherché à côté du binaire.
            ContentRootPath = AppContext.BaseDirectory,
        });
        // stdout est réservé aux messages du protocole MCP : les journaux partent sur stderr.
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddPortfolioApi(builder.Configuration);
        builder.Services.AddPortfolioMcpServer().WithStdioServerTransport();
        await builder.Build().RunAsync();
    }

    private static async Task RunHttpAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddPortfolioApi(builder.Configuration);
        // Sans état : chaque requête est autonome, aucune session à conserver entre deux instances ou redémarrages.
        builder.Services.AddPortfolioMcpServer().WithHttpTransport(o => o.Stateless = true);
        builder.Services.Configure<McpAccessOptions>(builder.Configuration.GetSection(McpAccessOptions.SectionName));

        var app = builder.Build();

        if (string.IsNullOrWhiteSpace(app.Configuration[$"{McpAccessOptions.SectionName}:AccessKey"]))
        {
            if (!app.Environment.IsDevelopment())
            {
                throw new InvalidOperationException("Mcp:AccessKey doit être configurée pour exposer le serveur MCP en HTTP.");
            }
            app.Logger.LogWarning("Mcp:AccessKey absente : point d'accès /mcp ouvert (développement uniquement).");
        }

        app.MapGet("/health", () => Results.Ok("ok"));
        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/mcp"), branch => branch.UseMiddleware<AccessKeyMiddleware>());
        app.MapMcp("/mcp");

        await app.RunAsync();
    }
}
