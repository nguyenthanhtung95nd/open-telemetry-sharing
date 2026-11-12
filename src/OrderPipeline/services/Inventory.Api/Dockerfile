FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/Inventory.Api/Inventory.Api.csproj", "services/Inventory.Api/"]
COPY ["shared/Shared.Observability/Shared.Observability.csproj", "shared/Shared.Observability/"]
COPY ["shared/Shared.Security/Shared.Security.csproj", "shared/Shared.Security/"]
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
RUN dotnet restore "services/Inventory.Api/Inventory.Api.csproj"
COPY . .
WORKDIR "/src/services/Inventory.Api"
RUN dotnet build "Inventory.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Inventory.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
