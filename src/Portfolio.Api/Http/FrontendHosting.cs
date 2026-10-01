using Microsoft.Net.Http.Headers;

namespace Portfolio.Api.Http;

/// <summary>
/// Sert le front React compilé (<c>wwwroot</c>) depuis l'API, pour un déploiement en un seul
/// service (Render). Sans <c>wwwroot/index.html</c> (développement, docker compose avec nginx),
/// rien n'est activé et le comportement de l'API est inchangé.
/// </summary>
public static class FrontendHosting
{
    // Mêmes en-têtes que nginx.conf : la CSP n'autorise que la même origine.
    private const string ContentSecurityPolicy =
        "default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'";

    public static bool HasFrontend(this IWebHostEnvironment environment) =>
        environment.WebRootFileProvider.GetFileInfo("index.html").Exists;

    public static WebApplication UseFrontend(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers[HeaderNames.XContentTypeOptions] = "nosniff";
            headers[HeaderNames.XFrameOptions] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers[HeaderNames.ContentSecurityPolicy] = ContentSecurityPolicy;
            await next();
        });

        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // Fichiers de /assets nommés par empreinte : cache long ; index.html toujours revalidé.
                ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                    ctx.Context.Request.Path.StartsWithSegments("/assets")
                        ? "public, max-age=31536000, immutable"
                        : "no-cache";
            },
        });
        return app;
    }

    /// <summary>
    /// Application monopage : toute route inconnue hors <c>/api</c> renvoie index.html. Une route
    /// d'API inconnue garde sa réponse 404 en ProblemDetails.
    /// </summary>
    public static WebApplication MapFrontendFallback(this WebApplication app)
    {
        app.MapFallback(async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
            await context.Response.SendFileAsync(app.Environment.WebRootFileProvider.GetFileInfo("index.html"));
        });
        return app;
    }
}
