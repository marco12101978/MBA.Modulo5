FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto (corrigidos)
COPY ["pagamentos-api/src/Pagamentos.API/Pagamentos.API.csproj", "pagamentos-api/src/Pagamentos.API/"]
COPY ["pagamentos-api/src/Pagamentos.Application/Pagamentos.Application.csproj", "pagamentos-api/src/Pagamentos.Application/"]
COPY ["pagamentos-api/src/Pagamentos.Domain/Pagamentos.Domain.csproj", "pagamentos-api/src/Pagamentos.Domain/"]
COPY ["pagamentos-api/src/Pagamentos.Infrastructure/Pagamentos.Infrastructure.csproj", "pagamentos-api/src/Pagamentos.Infrastructure/"]

# Restaurar dependências
RUN dotnet restore "pagamentos-api/src/Pagamentos.API/Pagamentos.API.csproj"

# Copiar código fonte
COPY . .

# Build da aplicação
WORKDIR "/src/pagamentos-api/src/Pagamentos.API"
RUN dotnet build "Pagamentos.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pagamentos.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Definir variáveis de ambiente para HTTP apenas
ENV ASPNETCORE_URLS="http://+:5004"

ENTRYPOINT ["dotnet", "Pagamentos.API.dll"] 