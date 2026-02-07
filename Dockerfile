# Using .NET 10 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy everything
COPY . ./

# Restore dependencies
RUN dotnet restore


# Build and publish the web project
RUN dotnet publish src/Chirp.Web/Chirp.Web.csproj -c Release -o out

# Build runtime image using .NET 10 ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/out .

# Expose port
EXPOSE 8080

# Set environment variable for ASP.NET
#ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Chirp.Web.dll"]