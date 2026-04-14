---
inclusion: always
---

# Project Structure - NovaHub

## Solution Layout

```
src/
├── Domain/           # Entidades, tipos base — sin dependencias externas
├── Application/      # Interfaces (IRepository) + Services (lógica de negocio)
├── Infrastructure/   # EF Core DbContext, Repositorios, Migraciones, DI
├── Server/           # ASP.NET Core Web API — Controllers, Hubs, Program.cs
├── Shared/           # DTOs compartidos entre Server y Client
└── Client/           # Blazor WASM SPA
```

## Reglas de capas
- Domain no tiene dependencias externas. Entidades heredan BaseEntity (Id, Activo).
- Application depende solo de Domain. Servicios mapean entidades a DTOs manualmente (sin AutoMapper).
- Infrastructure implementa interfaces de Application. Todo el DI vive en DependencyInjection.cs.
- Server controllers son delgados: inyectan el Service de Application, llaman, retornan IActionResult.
- Shared DTOs son los únicos tipos que cruzan el límite API — nunca exponer entidades de dominio.
- Client nunca referencia Application ni Infrastructure.

## Convenciones de nombres
- Entidades: singular PascalCase (Colaborador, Capacitacion)
- DTOs: {Entidad}Dto, Create{Entidad}Dto, Update{Entidad}Dto en Shared/DTOs/{Entidades}/
- Repositorios: interfaz I{Entidad}Repository + implementación {Entidad}Repository
- Servicios: {Entidad}Service en Application/Services/
- Controllers: {Entidades}Controller con ruta api/v1/[controller]
- Client API services: {Entidad}ApiService en Client/Services/

## Soft Deletes
Todas las entidades principales usan soft delete via flag Activo (de BaseEntity).
Los global query filters de EF excluyen Activo = false automáticamente.
DeleteAsync establece Activo = false — nunca hard delete.

Entidades que NO tienen soft delete (no heredan BaseEntity):
VersionDocumento, FlujoAprobacionDoc, PropuestaModificacion, AuditLog,
RespuestaCuestionario, RespuestaPregunta

## Estructura del Client

```
Client/
├── Pages/        # Componentes Razor enrutables (@page), uno por entidad + vistas de detalle
├── Shared/       # Componentes reutilizables: Toast, ConfirmDialog, EditorPlantilla, OneDriveBrowser
├── Layout/       # MainLayout (sidebar + topbar), LoginDisplay
├── Services/     # {Entidad}ApiService — wrappers de HttpClient con System.Net.Http.Json
└── wwwroot/      # Assets estáticos, app.css (CSS propio, sin librería de componentes)
```

### Componentes Shared disponibles
- Toast @ref="toast" — feedback de operaciones. Llamar: await toast.Show("msg") o await toast.Show("msg", "error") o await toast.Show("msg", "warning")
- ConfirmDialog Visible="..." Message="..." OnConfirm="..." OnCancel="..." — confirmación antes de acciones destructivas. Parámetro opcional: Title (default "¿Eliminar registro?")
- EditorPlantilla — editor HTML para plantillas de cartas laborales
- OneDriveBrowser — selector de archivos de OneDrive

### Namespaces globales (_Imports.razor)
Todos los namespaces de TalentManagement.Client.*, TalentManagement.Shared.DTOs.*
y los de ASP.NET Core están importados globalmente. No agregar @using por archivo para estos.

## Patrones UI de Blazor
- Las páginas manejan su propio estado (lista, modal abierto/cerrado, modelo de formulario, flags de carga)
- Formularios usan EditForm + DataAnnotationsValidator + ValidationMessage
- Feedback via Toast @ref="toast"
- Acciones destructivas usan ConfirmDialog antes de ejecutar
- CSS usa clases utilitarias propias en wwwroot/css/app.css:
  .card, .card-header, .card-title, .btn, .btn-primary, .btn-danger,
  .btn-ghost, .btn-sm, .btn-icon, .badge, .badge-warning, .badge-pill,
  .modal, .table-wrapper, .empty-state, .empty-state-icon,
  .loading, .spinner, .flex, .gap-2

## Patrones de API
- Todos los endpoints requieren [Authorize]
- Rutas siguen api/v1/[controller]
- Tipos de retorno: Ok(), CreatedAtAction(), NotFound(), NoContent(), UnprocessableEntity()
- Client services usan GetFromJsonAsync, PostAsJsonAsync, PutAsJsonAsync de System.Net.Http.Json
- Para uploads de archivo: multipart/form-data con IBrowserFile como StreamContent

## SignalR
- Hub: NotificacionesHub en /hubs/notificaciones
- Client service: NotificacionesService — se conecta al hub para notificaciones en tiempo real
