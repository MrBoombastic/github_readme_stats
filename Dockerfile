# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy solution and project files for restoration
COPY src/*.sln src/
COPY src/GitHubStats.Api/*.csproj src/GitHubStats.Api/
COPY src/GitHubStats.Application/*.csproj src/GitHubStats.Application/
COPY src/GitHubStats.Domain/*.csproj src/GitHubStats.Domain/
COPY src/GitHubStats.Infrastructure/*.csproj src/GitHubStats.Infrastructure/
COPY src/GitHubStats.Rendering/*.csproj src/GitHubStats.Rendering/

# Restore dependencies
RUN dotnet restore src/GitHubStats.sln

# Copy the rest of the source code
COPY src/ src/

# Build
WORKDIR /source/src/GitHubStats.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GitHubStats.Api.dll"]
