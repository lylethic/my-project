# Run API in here:

- cd `myproject\Play.APIs`
- run: `dotnet run` or `dotnet watch run`
- docker: `build: docker-compose build | run: docker-compose up`

# Project Structure

```
myproject/
├── Play.APIs/
| ├── Configuration/
│ ├── Controllers/
│ ├── Middleware/
│ ├── Properties/
│ ├── appsettings.Development.json
│ ├── appsettings.json
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
| ├── AutoMappers/
│ ├── Data/
| ├── Common/
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
```

## Explanation of the Structure

### Play.APIs/

Contains the API layer, with subdirectories for:

- **Configuration/**: These settings can include things like database connection strings, API keys, logging configurations, and environment-specific settings.
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

- **AutoMappers/**: It maps the properties of two different objects by transforming the input object of one type to the output object of another.
- **Common/**: common setting such as Contracts, Helpers, Utils,...
- **Data/**: Data access layer implementations.
- **Helpers/**: Utility classes and methods.
- **Migrations/**: Database migration files.
- **Common/**: Contractsm Helpers, Repositories(SimpleCrudRepositories), Databases(SqlCommandHelper).
- **Service/**: eg: RoleService,...
- **Repository/**: Implementation of repository patterns.
- **Play.Infrastructure.csproj**: The project file for the infrastructure layer.
