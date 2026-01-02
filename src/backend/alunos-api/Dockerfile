
# ==================================
# Stage 1: Base runtime image
# ==================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5003

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
COPY ["alunos-api/src/Alunos.API/Alunos.API.csproj", "alunos-api/src/Alunos.API/"]
COPY ["alunos-api/src/Alunos.Application/Alunos.Application.csproj", "alunos-api/src/Alunos.Application/"]
COPY ["alunos-api/src/Alunos.Domain/Alunos.Domain.csproj", "alunos-api/src/Alunos.Domain/"]
COPY ["alunos-api/src/Alunos.Infrastructure/Alunos.Infrastructure.csproj", "alunos-api/src/Alunos.Infrastructure/"]
COPY ["building-blocks/core/Core.csproj", "building-blocks/core/"]
COPY ["building-blocks/MessageBus/MessageBus.csproj", "building-blocks/MessageBus/"]

# Restaurar dependências (camada cacheável)
RUN dotnet restore "alunos-api/src/Alunos.API/Alunos.API.csproj"

# Copiar o restante do código fonte
COPY alunos-api/ alunos-api/
COPY building-blocks/ building-blocks/

# Build da aplicação
WORKDIR "/src/alunos-api/src/Alunos.API"
RUN dotnet build "Alunos.API.csproj" -c Release -o /app/build

# ==================================
# Stage 3: Publish stage
# ==================================
FROM build AS publish
RUN dotnet publish "Alunos.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
    ASPNETCORE_URLS=http://+:5003 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:5003/health || exit 1

ENTRYPOINT ["dotnet", "Alunos.API.dll"] 