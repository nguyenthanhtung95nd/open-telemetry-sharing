FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/Gateway.Api/Gateway.Api.csproj", "services/Gateway.Api/"]
COPY ["shared/Shared.Observability/Shared.Observability.csproj", "shared/Shared.Observability/"]
RUN dotnet restore "services/Gateway.Api/Gateway.Api.csproj"
COPY . .
WORKDIR "/src/services/Gateway.Api"
RUN dotnet build "Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
