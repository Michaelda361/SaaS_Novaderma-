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

## Correr el proyecto localmente

Siempre arrancar Server primero, luego Client en otra terminal:

```bash
# Terminal 1
dotnet run --project src/Server

# Terminal 2
dotnet run --project src/Client
```

## Usuarios de desarrollo (sin MSAL)

En Development, la autenticación real de Azure Entra ID está desactivada. Se usa `DevAuthHandler` con el header `X-Dev-User` o el `DevUserStore`.

| Email | Rol |
|---|---|
| `dev.colaborador@test.local` | Colaborador normal (acceso restringido) |
| `dev.jefe@test.local` | Jefe de área Tecnología |
| `dev.jeferrhh@test.local` | Jefe de área RRHH |

El usuario activo se configura en `appsettings.Development.json`:
```json
"DevSettings": {
  "DefaultDevUser": "dev.jefe@test.local"
}
```

También se puede cambiar en caliente desde la UI en `/dev` (visible solo en Development).

## EF Migrations

```bash
# Agregar migración (desde raíz del repo)
dotnet ef migrations add <NombreMigracion> --project src/Infrastructure --startup-project src/Server

# Aplicar al DB
dotnet ef database update --project src/Infrastructure --startup-project src/Server
```

Migraciones viven en `src/Infrastructure/Migrations/`. Hay 13 migraciones activas desde `InitialCreate`.

## Seeding automático

`DbSeeder.SeedAsync()` corre automáticamente al iniciar en Development si la BD está vacía. Crea Areas, Cargos y 3 Colaboradores de prueba.

## Configuración requerida

Copiar `src/Server/appsettings.example.json` → `appsettings.Development.json` y completar:
- `ConnectionStrings:DefaultConnection` — SQL Server local
- `AzureAd` — tenant/client ID (solo necesario si se prueba con MSAL real)

## SharePoint / OneDrive en dev

En Development se usan mocks automáticamente:
- `MockSharePointService` en lugar de `SharePointService`
- `MockAuditExcelService` en lugar de `AuditExcelService`

No se necesita configuración de Graph API para desarrollo local.

## Autorización en Client

El Client usa `DevAwareAuthorizationMessageHandler` que detecta si está en dev y agrega el header `X-Dev-User` en lugar del Bearer token de MSAL.

Dos cascading parameters disponibles en todas las páginas (desde MainLayout):
- `EsMicrosoftUser` — true si el usuario autenticado es de Azure AD
- `EsSoloColaborador` — true si el usuario solo tiene rol de colaborador (acceso restringido)
