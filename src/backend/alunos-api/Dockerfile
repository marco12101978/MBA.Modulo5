FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
COPY ["alunos-api/src/Alunos.API/Alunos.API.csproj", "alunos-api/src/Alunos.API/"]
COPY ["alunos-api/src/Alunos.Application/Alunos.Application.csproj", "alunos-api/src/Alunos.Application/"]
COPY ["alunos-api/src/Alunos.Domain/Alunos.Domain.csproj", "alunos-api/src/Alunos.Domain/"]
COPY ["alunos-api/src/Alunos.Infrastructure/Alunos.Infrastructure.csproj", "alunos-api/src/Alunos.Infrastructure/"]
COPY ["building-blocks/core/Core.csproj", "building-blocks/core/"]
COPY ["building-blocks/MessageBus/MessageBus.csproj", "building-blocks/MessageBus/"]

# Restaurar dependências
RUN dotnet restore "alunos-api/src/Alunos.API/Alunos.API.csproj"

# Copiar código fonte
COPY . .

# Build da aplicação
WORKDIR "/src/alunos-api/src/Alunos.API"
RUN dotnet build "Alunos.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Alunos.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Definir variáveis de ambiente para HTTP apenas
ENV ASPNETCORE_URLS="http://+:5003"

ENTRYPOINT ["dotnet", "Alunos.API.dll"] 