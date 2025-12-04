FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
COPY ["src/Auth.API/Auth.API.csproj", "src/Auth.API/"]
COPY ["src/Auth.Application/Auth.Application.csproj", "src/Auth.Application/"]
COPY ["src/Auth.Domain/Auth.Domain.csproj", "src/Auth.Domain/"]
COPY ["src/Auth.Infrastructure/Auth.Infrastructure.csproj", "src/Auth.Infrastructure/"]

# Restaurar dependências
RUN dotnet restore "src/Auth.API/Auth.API.csproj"

# Copiar código fonte
COPY . .

# Build da aplicação
WORKDIR "/src/src/Auth.API"
RUN dotnet build "Auth.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Auth.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Auth.API.dll"] 