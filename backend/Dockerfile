# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /src
COPY Cinema.Api/Cinema.Api.csproj Cinema.Api/
COPY Cinema.Application/Cinema.Application.csproj Cinema.Application/
COPY Cinema.Domain/Cinema.Domain.csproj Cinema.Domain/
COPY Cinema.Infrastructure/Cinema.Infrastructure.csproj Cinema.Infrastructure/
COPY Cinema.Shared/Cinema.Shared.csproj Cinema.Shared/
RUN dotnet restore Cinema.Api/Cinema.Api.csproj

FROM restore AS build
COPY . .
RUN dotnet publish Cinema.Api/Cinema.Api.csproj -c Release -o /app/publish --no-restore

FROM restore AS dev
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libfontconfig1 tzdata \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
COPY . .
EXPOSE 8080
ENTRYPOINT ["dotnet", "watch", "--project", "Cinema.Api", "run", "--no-launch-profile", "--non-interactive", "--no-hot-reload"]

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS production
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build --chown=app:app /app/publish .
RUN mkdir -p /app/logs && chown -R app:app /app/logs
USER app

#   384 MB = 0x18000000.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=0 \
    DOTNET_GCConserveMemory=9 \
    DOTNET_GCHeapHardLimit=0x18000000 \
    DOTNET_TieredPGO=0
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cinema.Api.dll"]
