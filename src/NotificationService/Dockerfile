# --- Stage 1: build & publish ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NotificationService.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app

# --- Stage 2: lightweight runtime image ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NotificationService.dll"]
