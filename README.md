# Project Structure

myproject/
├── Play.APIs/
│ ├── Controllers/
│ ├── Middleware/
│ ├── Properties/
│ ├── appsettings-example.json
│ ├── appsettings.Development.json
│ ├── appsettings.json
│ ├── Play.APIs.csproj
│ ├── Play.APIs.csproj.user
│ ├── Play.APIs.http
│ └── Program.cs
│
├── Play.Application/
│ ├── DTOs/
│ ├── IRepository/
│ ├── Extensions.cs
│ └── Play.Application.csproj
│
├── Play.Domain/
│ ├── Entities/
│ └── Play.Domain.csproj
│
├── Play.Infrastructure/
│ ├── Data/
│ ├── Helpers/
│ ├── Migrations/
│ ├── Repository/
│ └── Play.Infrastructure.csproj
│
├── .gitignore
│
├── docker-compose.yaml
│
├── Dockerfile
│
├── Play.API.sln
│
└── README.md

## Explanation of the Structure

### Play.APIs/

Contains the API layer, with subdirectories for:

- **Controllers/**: Handles incoming requests and returns responses.
- **Middleware/**: Custom middleware components for processing requests.
- **Properties/**: Contains project properties.
- **Configuration Files**: Includes various appsettings files for different environments.

### Play.Application/

Holds the application logic with:

- **DTOs/**: Data Transfer Objects used for data exchange.
- **IRepository/**: Interfaces defining repository methods.
- **Extensions.cs**: Extension methods for enhancing functionality.

### Play.Domain/

Contains domain entities and the project file:

- **Entities/**: Represents the core business models.
- **Play.Domain.csproj**: The project file for the domain layer.

### Play.Infrastructure/

Manages data access and related components:

- **Data/**: Data access layer implementations.
- **Helpers/**: Utility classes and methods.
- **Migrations/**: Database migration files.
- **Repository/**: Implementation of repository patterns.
- **Play.Infrastructure.csproj**: The project file for the infrastructure layer.

### Migration database

- dotnet ef migrations add Init --project .\Play.Infrastructure\Play.Infrastructure.csproj --startup-project .\Play.APIs\Play.APIs.csproj --output-dir Migrations

- Key Parameters:
  --project: Infrastructure project path
  --startup-project: API project containing appsettings.json
  --output-dir: Migration folder location

### Update Database

- dotnet ef database update --project .\Play.Infrastructure\Play.Infrastructure.csproj --startup-project .\Play.APIs\Play.APIs.csproj
