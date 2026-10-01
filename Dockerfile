# Image « tout-en-un » pour Render : l'API ASP.NET Core sert aussi le front React compilé.
# (docker compose garde son architecture à trois conteneurs : nginx, API, PostgreSQL.)
# Contexte de build : racine du dépôt.

# --- Front React ---------------------------------------------------------------------------
FROM node:24-alpine AS web
WORKDIR /web
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci
COPY src/web/ ./
RUN npm run build

# --- API .NET ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src

# Restauration d'abord, pour profiter du cache Docker tant que les .csproj ne changent pas.
COPY src/Portfolio.Domain/Portfolio.Domain.csproj src/Portfolio.Domain/
COPY src/Portfolio.Application/Portfolio.Application.csproj src/Portfolio.Application/
COPY src/Portfolio.Infrastructure/Portfolio.Infrastructure.csproj src/Portfolio.Infrastructure/
COPY src/Portfolio.Api/Portfolio.Api.csproj src/Portfolio.Api/
RUN dotnet restore src/Portfolio.Api/Portfolio.Api.csproj

COPY src/Portfolio.Domain/ src/Portfolio.Domain/
COPY src/Portfolio.Application/ src/Portfolio.Application/
COPY src/Portfolio.Infrastructure/ src/Portfolio.Infrastructure/
COPY src/Portfolio.Api/ src/Portfolio.Api/
RUN dotnet publish src/Portfolio.Api/Portfolio.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

# --- Image finale --------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=api /app .
# La présence de wwwroot/index.html active le service du front (voir Http/FrontendHosting.cs).
COPY --from=web /web/dist ./wwwroot

# Utilisateur non root fourni par l'image officielle ; écoute sur 8080 (ASPNETCORE_HTTP_PORTS).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Portfolio.Api.dll"]
