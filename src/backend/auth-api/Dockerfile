# ==================================
# Stage 1: Base runtime image
# ==================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

# Instalar curl para health checks e criar usuário não-root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    useradd -m -u 1000 -s /bin/bash appuser && \
    chown -R appuser:appuser /app

# ==================================
# Stage 2: Build stage
# ==================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar apenas os arquivos de projeto primeiro (camada cacheável)
COPY ["auth-api/src/Auth.API/Auth.API.csproj", "auth-api/src/Auth.API/"]
COPY ["auth-api/src/Auth.Application/Auth.Application.csproj", "auth-api/src/Auth.Application/"]
COPY ["auth-api/src/Auth.Domain/Auth.Domain.csproj", "auth-api/src/Auth.Domain/"]
COPY ["auth-api/src/Auth.Infrastructure/Auth.Infrastructure.csproj", "auth-api/src/Auth.Infrastructure/"]
COPY ["building-blocks/core/Core.csproj", "building-blocks/core/"]
COPY ["building-blocks/MessageBus/MessageBus.csproj", "building-blocks/MessageBus/"]
COPY ["Directory.Build.props", "./"]

# Restaurar dependências (camada cacheável)
RUN dotnet restore "auth-api/src/Auth.API/Auth.API.csproj"

# Copiar o restante do código fonte
COPY auth-api/ auth-api/
COPY building-blocks/ building-blocks/

# Build da aplicação
WORKDIR "/src/auth-api/src/Auth.API"
RUN dotnet build "Auth.API.csproj" -c Release -o /app/build

# ==================================
# Stage 3: Publish stage
# ==================================
FROM build AS publish
RUN dotnet publish "Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==================================
# Stage 4: Final runtime image
# ==================================
FROM base AS final
WORKDIR /app

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Criar diretório para dados e ajustar permissões
RUN mkdir -p /app/data && chown -R appuser:appuser /app/data

# Mudar para usuário não-root
USER appuser

# Variáveis de ambiente (podem ser sobrescritas)
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5001 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:5001/health || exit 1

ENTRYPOINT ["dotnet", "Auth.API.dll"] 