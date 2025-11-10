# Debug/fast mode base (needs ASP.NET Core runtime because of Serilog.AspNetCore)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# (optional) remove the next line unless you set APP_UID elsewhere:
# USER $APP_UID
WORKDIR /app

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MarketInsight.Ingestor/MarketInsight.Ingestor.csproj", "MarketInsight.Ingestor/"]
RUN dotnet restore "MarketInsight.Ingestor/MarketInsight.Ingestor.csproj"
COPY . .
WORKDIR "/src/MarketInsight.Ingestor"
RUN dotnet build "MarketInsight.Ingestor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MarketInsight.Ingestor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final/runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarketInsight.Ingestor.dll"]
