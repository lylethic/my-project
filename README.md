# Project Structure

myproject/
├── Play.APIs.csproj (Startup Project)
└── appsettings.json

Play.Infrastructure/
├── Data/
│ ├── ApiDbContext.cs
│ └── Migrations/ (Target location)
└── Play.Infrastructure.csproj (EF Core project)

### Migration database

- dotnet ef migrations add Init --project .\Play.Infrastructure\Play.Infrastructure.csproj --startup-project .\Play.APIs\Play.APIs.csproj --output-dir Migrations

- Key Parameters:
  --project: Infrastructure project path
  --startup-project: API project containing appsettings.json
  --output-dir: Migration folder location

### Update Database

- dotnet ef database update --project .\Play.Infrastructure\Play.Infrastructure.csproj --startup-project .\Play.APIs\Play.APIs.csproj
