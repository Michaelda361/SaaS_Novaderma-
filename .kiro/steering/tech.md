# Tech Stack

## Frameworks & Runtime
- **.NET 10** — all projects target `net10.0`
- **ASP.NET Core Web API** — REST backend (`TalentManagement.Server`)
- **Blazor WebAssembly** — SPA frontend (`TalentManagement.Client`)
- **Entity Framework Core 10** — ORM with SQL Server provider

## Auth
- **Microsoft.Identity.Web** — server-side JWT validation via Azure Entra ID
- **Microsoft.Authentication.WebAssembly.Msal** — MSAL for Blazor WASM, redirect login flow
- All API routes are protected with `[Authorize]`

## Key Libraries
| Package | Purpose |
|---|---|
| `Microsoft.AspNetCore.OpenApi` | OpenAPI spec generation |
| `Scalar.AspNetCore` | API reference UI (dev only) |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server EF provider |
| `Microsoft.EntityFrameworkCore.Design` | EF migrations tooling |

## Common Commands

### Build
```bash
dotnet build
```

### Run Server (API)
```bash
dotnet run --project src/Server
```

### Run Client (Blazor WASM)
```bash
dotnet run --project src/Client
```

### EF Migrations (run from repo root)
```bash
# Add migration
dotnet ef migrations add <MigrationName> --project src/Infrastructure --startup-project src/Server

# Apply migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Server
```

## Configuration
- Connection string: `DefaultConnection` in `appsettings.json`
- Azure AD settings: `AzureAd` section in `appsettings.json`
- See `src/Server/appsettings.example.json` for the expected shape
- Dev seeding runs automatically on startup when `ASPNETCORE_ENVIRONMENT=Development`
