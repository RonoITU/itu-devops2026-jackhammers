FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Chirp.Web/Chirp.Web.csproj", "src/Chirp.Web/"]
COPY ["src/Chirp.Infrastructure/Chirp.Infrastructure.csproj", "src/Chirp.Infrastructure/"]
COPY ["src/Chirp.Core/Chirp.Core.csproj", "src/Chirp.Core/"]
RUN dotnet restore "src/Chirp.Web/Chirp.Web.csproj"
COPY . .
WORKDIR "/src/src/Chirp.Web"
RUN dotnet build "./Chirp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Chirp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Chirp.Web.dll"]