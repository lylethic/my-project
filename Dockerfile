# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy solution file
COPY ["Play.API.sln", "./"]

# Copy all project files for proper dependency resolution
COPY ["Play.APIs/Play.APIs.csproj", "Play.APIs/"]
COPY ["Play.Application/Play.Application.csproj", "Play.Application/"]
COPY ["Play.Domain/Play.Domain.csproj", "Play.Domain/"]
COPY ["Play.Infrastructure/Play.Infrastructure.csproj", "Play.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Play.APIs/Play.APIs.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "Play.APIs/Play.APIs.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build-env /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "Play.APIs.dll"]