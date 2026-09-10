# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar os arquivos de projeto da solução
COPY ["eloja.API/eloja.API.csproj", "eloja.API/"]
COPY ["eloja.IoC/eloja.IoC.csproj", "eloja.IoC/"]
COPY ["eloja.Services/eloja.Services.csproj", "eloja.Services/"]
COPY ["eloja.Data/eloja.Data.csproj", "eloja.Data/"]
COPY ["eloja.API.sln", "./"]

# Restaurar dependências
RUN dotnet restore "eloja.API.sln"

# Copiar todo o código-fonte
COPY . .

# Publicar a aplicação
RUN dotnet publish "eloja.API/eloja.API.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Copiar os arquivos publicados do estágio de build
COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "eloja.API.dll"]
