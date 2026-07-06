# --- Stage 1: build & publish ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OrderService.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app

# --- Stage 2: lightweight runtime image ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# curl: used by the docker-compose healthcheck to probe /health (Phase 5).
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderService.dll"]
