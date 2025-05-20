# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY ["Play.APIs/Play.APIs.csproj", "Play.APIs/"]
RUN dotnet restore "Play.APIs/Play.APIs.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "Play.APIs/Play.APIs.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build-env /app/publish .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "Play.APIs.dll"]