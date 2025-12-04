FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /

# Copiar arquivos de projeto
COPY ["conteudo-api/src/Conteudo.API/Conteudo.API.csproj", "conteudo-api/src/Conteudo.API/"]
COPY ["conteudo-api/src/Conteudo.Application/Conteudo.Application.csproj", "conteudo-api/src/Conteudo.Application/"]
COPY ["conteudo-api/src/Conteudo.Domain/Conteudo.Domain.csproj", "conteudo-api/src/Conteudo.Domain/"]
COPY ["conteudo-api/src/Conteudo.Infrastructure/Conteudo.Infrastructure.csproj", "conteudo-api/src/Conteudo.Infrastructure/"]
COPY ["building-blocks/core/Core.csproj", "building-blocks/core/"]

# Restaurar dependências
RUN dotnet restore "conteudo-api/src/Conteudo.API/Conteudo.API.csproj"

# Copiar código fonte
COPY . .

# Build da aplicação
WORKDIR "/conteudo-api/src/Conteudo.API"
RUN dotnet build "Conteudo.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Conteudo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Definir variáveis de ambiente para HTTP apenas
ENV ASPNETCORE_URLS="http://+:5002"

ENTRYPOINT ["dotnet", "Conteudo.API.dll"] 