# ===============================
# BASE (runtime)
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
# curl gerekli
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# ===============================
# BUILD
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV NUGET_HTTP_TIMEOUT_SECONDS=300 \
    NUGET_ENHANCED_MAX_NETWORK_TRY_COUNT=8 \
    NUGET_ENHANCED_NETWORK_RETRY_DELAY_MILLISECONDS=2000
WORKDIR /src
COPY ["ECommerce.API/ECommerce.API.csproj", "ECommerce.API/"]
COPY ["ECommerce.Adaptor/ECommerce.Adaptor.csproj", "ECommerce.Adaptor/"]
COPY ["ECommerce.Repository/ECommerce.Repository.csproj", "ECommerce.Repository/"]
COPY ["ECommerce.Domain/ECommerce.Domain.csproj", "ECommerce.Domain/"]
COPY ["ECommerce.Service/ECommerce.Service.csproj", "ECommerce.Service/"]
# Coolify sunucusunda nuget.org yanıtı zaman zaman yavaşlayabiliyor.
# Paralel olmayan ve tekrar deneyen restore, geçici ağ gecikmelerinde build'in düşmesini önler.
RUN dotnet restore "ECommerce.API/ECommerce.API.csproj" --disable-parallel
COPY . .
WORKDIR /src/ECommerce.API
RUN dotnet publish "ECommerce.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ===============================
# FINAL
# ===============================
FROM base AS final
WORKDIR /app
RUN mkdir -p /app/wwwroot/uploads/images
COPY --from=build /app/publish .

# DİKKAT: Port 80 olarak değiştirildi (docker-compose ile uyumlu)
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
