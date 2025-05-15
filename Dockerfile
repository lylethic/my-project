# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY ["myproject/myproject.csproj", "myproject/"]
RUN dotnet restore "myproject/myproject.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "myproject/myproject.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build-env /app/publish .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "myproject.dll"]