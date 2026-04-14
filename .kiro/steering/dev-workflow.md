---
inclusion: always
---

# Dev Workflow — NovaHub

## Puertos y URLs

| Proyecto | URL |
|---|---|
| Server (API) | http://localhost:5194 |
| Client (Blazor WASM) | http://localhost:5185 |
| Scalar API UI | http://localhost:5194/scalar/v1 |
| SignalR Hub | http://localhost:5194/hubs/notificaciones |

## Correr el proyecto localmente

Siempre arrancar Server primero, luego Client en otra terminal:

`dotnet run --project src/Server`
`dotnet run --project src/Client`

## Usuarios de desarrollo (sin MSAL)

En Development, la autenticación real de Azure Entra ID está desactivada. Se usa `DevAuthHandler`
con el header `X-Dev-User` o el `DevUserStore`.

| Email | Rol | Descripción |
|---|---|---|
| `dev.colaborador@test.local` | Colaborador | Acceso restringido |
| `dev.jefe@test.local` | Jefe | Jefe de área Tecnología |
| `dev.jeferrhh@test.local` | Jefe | Jefe de área RRHH |

El usuario activo se configura en `appsettings.Development.json`:
`"DevSettings": { "DefaultDevUser": "dev.jefe@test.local" }`

También se puede cambiar en caliente desde el selector en el sidebar (visible solo en Development).

## Perfil del usuario autenticado

El endpoint `GET /api/v1/auth/perfil` resuelve el colaborador por email y retorna:
- `email`, `esColaborador`, `esJefe`, `colaboradorId`, `esDevUser`, `rol`

El Client lo consume vía `AuthInfoService` que expone:
- `EsMicrosoftUser` — true si el usuario autenticado es de Azure AD (no dev)
- `EsSoloColaborador` — true si el rol es Colaborador (acceso restringido)

Ambos están disponibles como `CascadingParameter` en todas las páginas desde `MainLayout`.

## EF Migrations

`dotnet ef migrations add <NombreMigracion> --project src/Infrastructure --startup-project src/Server`
`dotnet ef database update --project src/Infrastructure --startup-project src/Server`

Migraciones viven en `src/Infrastructure/Migrations/`. Hay **16 migraciones** activas desde `InitialCreate`.
La más reciente: `AddRolToColaborador` (20260414).

### Convención de nombres de migración
- Nueva entidad: `Add{Entidad}`
- Nueva propiedad: `Add{Propiedad}To{Entidad}`
- Nueva relación: `Add{Relacion}`
- Cambio de tipo: `Change{Propiedad}TypeIn{Entidad}`

## Seeding automático

`DbSeeder.SeedAsync()` corre automáticamente al iniciar en Development si la BD está vacía.
Crea Areas, Cargos y 3 Colaboradores de prueba (uno por cada usuario dev).

## Configuración requerida

Copiar `src/Server/appsettings.example.json` → `appsettings.Development.json` y completar:
- `ConnectionStrings:DefaultConnection` — SQL Server local
- `AzureAd` — tenant/client ID (solo necesario si se prueba con MSAL real)
- `SharePoint` — TenantId, ClientId, ClientSecret, SiteUrl (solo producción; en dev se usa mock)

## Mocks en Development

En Development se usan mocks automáticamente (switch en `DependencyInjection.cs`):
- `MockSharePointService` en lugar de `SharePointService`
- `MockAuditExcelService` en lugar de `AuditExcelService`

No se necesita configuración de Graph API para desarrollo local.

## Autorización en Client

El Client usa `DevAwareAuthorizationMessageHandler` que detecta si está en dev y agrega
el header `X-Dev-User` en lugar del Bearer token de MSAL.