# --- Stage 1: build & publish ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first and restore — this layer is cached unless the csproj changes.
COPY ProductCatalogService.csproj ./
RUN dotnet restore

# Copy the rest of the source and publish a release build.
COPY . ./
RUN dotnet publish -c Release -o /app

# --- Stage 2: lightweight runtime image ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Kestrel listens on 8080 inside the container.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ProductCatalogService.dll"]
