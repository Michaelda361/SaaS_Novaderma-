# Project Structure

## Solution Layout

```
src/
├── Domain/           # Entities, base types — no dependencies on other layers
├── Application/      # Interfaces (IRepository) + Services (business logic)
├── Infrastructure/   # EF Core DbContext, Repositories, Migrations, DI registration
├── Server/           # ASP.NET Core Web API — Controllers, Program.cs
├── Shared/           # DTOs shared between Server and Client
└── Client/           # Blazor WASM SPA
```

## Layer Rules
- `Domain` has zero external dependencies. Entities inherit from `BaseEntity` (`Id`, `Activo`).
- `Application` depends only on `Domain`. Services map entities → DTOs manually (no AutoMapper).
- `Infrastructure` implements `Application` interfaces. All DI registration lives in `DependencyInjection.cs`.
- `Server` controllers are thin: inject the Application service, call it, return `IActionResult`.
- `Shared` DTOs are the only types crossing the API boundary — never expose domain entities directly.
- `Client` never references `Application` or `Infrastructure`.

## Naming Conventions
- Entities: singular PascalCase (`Colaborador`, `Capacitacion`)
- DTOs: `{Entity}Dto`, `Create{Entity}Dto`, `Update{Entity}Dto` in `Shared/DTOs/{Entity}s/`
- Repositories: `I{Entity}Repository` interface + `{Entity}Repository` implementation
- Services: `{Entity}Service` in `Application/Services/`
- Controllers: `{Entity}sController` with route `api/v1/[controller]`
- Client API services: `{Entity}ApiService` in `Client/Services/`

## Soft Deletes
All main entities use soft delete via `Activo` flag (from `BaseEntity`). EF global query filters exclude `Activo = false` records automatically. `DeleteAsync` sets `Activo = false` — never hard deletes.

## Client Structure
```
Client/
├── Pages/        # Routable Razor components (@page), one per entity + detail views
├── Shared/       # Reusable components (Toast, ConfirmDialog, etc.)
├── Layout/       # MainLayout, sidebar, topbar
├── Services/     # {Entity}ApiService — HttpClient wrappers using System.Net.Http.Json
└── wwwroot/      # Static assets, app.css (custom CSS, no component library)
```

## Blazor UI Patterns
- Pages manage their own state (list, modal open/close, form model, loading flags)
- Forms use `EditForm` + `DataAnnotationsValidator` + `ValidationMessage`
- Feedback via `<Toast @ref="toast" />` — call `toast.Show("message")` or `toast.Show("message", "error")`
- Destructive actions use `<ConfirmDialog>` before executing
- CSS uses custom utility classes defined in `wwwroot/css/app.css` (`.card`, `.btn`, `.badge`, `.modal`, `.table-wrapper`, `.empty-state`, etc.)
- All namespaces are globally imported via `_Imports.razor` — no per-file `@using` needed for common types

## API Patterns
- All endpoints require `[Authorize]`
- Routes follow `api/v1/[controller]`
- Return types: `Ok()`, `CreatedAtAction()`, `NotFound()`, `NoContent()`
- Client services use `GetFromJsonAsync`, `PostAsJsonAsync`, `PutAsJsonAsync` from `System.Net.Http.Json`
