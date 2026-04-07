FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy .csproj files for dotnet restore.
COPY ["src/Chirp.Web/Chirp.Web.csproj", "src/Chirp.Web/"]
COPY ["src/Chirp.Infrastructure/Chirp.Infrastructure.csproj", "src/Chirp.Infrastructure/"]
COPY ["src/Chirp.Core/Chirp.Core.csproj", "src/Chirp.Core/"]

RUN dotnet restore "src/Chirp.Web/Chirp.Web.csproj"

# Copy rest of source to prepare for publish step
COPY /src .

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Chirp.Web/Chirp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --ucr

FROM base AS final

USER root
RUN apk add --no-cache tzdata

USER app
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Chirp.Web.dll"]