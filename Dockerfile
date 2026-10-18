# CivicConnect container image for staging (and later production).
# Build:  docker build -t civicconnect .
# The image holds the web app and a migration bundle. Migrations are a separate deployment step
# (PED D-015): run ./efbundle once per release before the app starts. See docs/operations/deployment.md.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached while only source files change
COPY Directory.Build.props CivicConnect.slnx ./
COPY .config/dotnet-tools.json .config/
COPY src/CivicConnect.Modules/CivicConnect.Modules.csproj src/CivicConnect.Modules/
COPY src/CivicConnect.Infrastructure/CivicConnect.Infrastructure.csproj src/CivicConnect.Infrastructure/
COPY src/CivicConnect.Web/CivicConnect.Web.csproj src/CivicConnect.Web/
RUN dotnet restore src/CivicConnect.Web/CivicConnect.Web.csproj && dotnet tool restore

COPY src/ src/
RUN dotnet publish src/CivicConnect.Web/CivicConnect.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# The EF tool starts the app's service setup to read the model, which needs a connection string to
# exist. It never connects while building the bundle, so a placeholder without a password is enough.
RUN ConnectionStrings__CivicConnect="Host=placeholder;Database=build" \
    dotnet ef migrations bundle \
      --project src/CivicConnect.Infrastructure \
      --startup-project src/CivicConnect.Web \
      --configuration Release \
      --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/efbundle ./efbundle

# Staging by default; the hosting platform sets the real values (connection string, admin seed).
# FORWARDEDHEADERS_ENABLED makes the app trust the platform's HTTPS proxy, so redirects and
# cookies use https even though the container itself listens on plain http.
ENV ASPNETCORE_ENVIRONMENT=Staging \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080

# The base image's non-root user
USER $APP_UID

# Health: point the platform's health check at GET /health (returns "Healthy" when the database is reachable)
ENTRYPOINT ["dotnet", "CivicConnect.Web.dll"]
