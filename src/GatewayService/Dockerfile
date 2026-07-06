FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# curl: used by the docker-compose healthcheck to probe /health (Phase 5).
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GatewayService.csproj", "./"]
RUN dotnet restore "GatewayService.csproj"
COPY . .
RUN dotnet publish "GatewayService.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GatewayService.dll"]
