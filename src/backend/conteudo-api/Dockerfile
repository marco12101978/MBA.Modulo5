# ==================================
# Stage 1: Base runtime image
# ==================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5002

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
COPY ["conteudo-api/src/Conteudo.API/Conteudo.API.csproj", "conteudo-api/src/Conteudo.API/"]
COPY ["conteudo-api/src/Conteudo.Application/Conteudo.Application.csproj", "conteudo-api/src/Conteudo.Application/"]
COPY ["conteudo-api/src/Conteudo.Domain/Conteudo.Domain.csproj", "conteudo-api/src/Conteudo.Domain/"]
COPY ["conteudo-api/src/Conteudo.Infrastructure/Conteudo.Infrastructure.csproj", "conteudo-api/src/Conteudo.Infrastructure/"]
COPY ["building-blocks/core/Core.csproj", "building-blocks/core/"]
COPY ["building-blocks/MessageBus/MessageBus.csproj", "building-blocks/MessageBus/"]

# Restaurar dependências (camada cacheável)
RUN dotnet restore "conteudo-api/src/Conteudo.API/Conteudo.API.csproj"

# Copiar o restante do código fonte
COPY conteudo-api/ conteudo-api/
COPY building-blocks/ building-blocks/

# Build da aplicação
WORKDIR "/src/conteudo-api/src/Conteudo.API"
RUN dotnet build "Conteudo.API.csproj" -c Release -o /app/build

# ==================================
# Stage 3: Publish stage
# ==================================
FROM build AS publish
RUN dotnet publish "Conteudo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
    ASPNETCORE_URLS=http://+:5002 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:5002/health || exit 1

ENTRYPOINT ["dotnet", "Conteudo.API.dll"] 