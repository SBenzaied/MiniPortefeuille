# Serveur MCP en HTTP (Render). Contexte de build : racine du dépôt.
# Le serveur ne contient aucune donnée : il appelle l'API REST de Mini-portefeuille.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Portfolio.Mcp/Portfolio.Mcp.csproj src/Portfolio.Mcp/
RUN dotnet restore src/Portfolio.Mcp/Portfolio.Mcp.csproj
COPY src/Portfolio.Mcp/ src/Portfolio.Mcp/
RUN dotnet publish src/Portfolio.Mcp/Portfolio.Mcp.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Portfolio.Mcp.dll"]
