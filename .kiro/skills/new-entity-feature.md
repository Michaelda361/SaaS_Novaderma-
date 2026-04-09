# Skill: Nueva Entidad Completa — NovaHub

Guía paso a paso para agregar una entidad nueva de punta a punta. Seguir el orden exacto para evitar errores de compilación por dependencias entre capas.

## Parámetros de entrada

Antes de empezar, definir:
- `{Entidad}` — nombre singular PascalCase (ej: `Proveedor`)
- `{Entidades}` — nombre plural (ej: `Proveedores`)
- `{entidades}` — plural camelCase para rutas (ej: `proveedores`)
- Propiedades de la entidad y sus FKs

---

## Paso 1 — Domain: Entidad

Crear `src/Domain/Entities/{Entidad}.cs`:
- Heredar de `BaseEntity`
- Propiedades con valores por defecto (`= string.Empty`, `= null!`)
- Colecciones con `[JsonIgnore]` para evitar ciclos de serialización
- FKs con su propiedad de navegación

## Paso 2 — Shared: DTOs

Crear carpeta `src/Shared/DTOs/{Entidades}/` con 3 archivos:

**{Entidad}Dto.cs** — para respuestas de la API:
- Todas las propiedades que el cliente necesita ver
- Nombres de entidades relacionadas como `string?` (ej: `AreaNombre`)

**Create{Entidad}Dto.cs** — para crear:
- Propiedades editables con `[Required]`, `[EmailAddress]`, etc.
- Namespace: `TalentManagement.Shared.DTOs.{Entidades}`

**Update{Entidad}Dto.cs** — para editar:
- Generalmente hereda de `Create{Entidad}Dto` o tiene las mismas propiedades

## Paso 3 — Application: Interfaz del repositorio

Crear `src/Application/Interfaces/I{Entidad}Repository.cs`:
- Métodos mínimos: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ExistsAsync`
- Agregar métodos de consulta específicos si se necesitan (ej: `GetByAreaAsync`)

## Paso 4 — Application: Service

Crear `src/Application/Services/{Entidad}Service.cs`:
- Constructor con primary constructor: `(I{Entidad}Repository repository)`
- Métodos: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- Mapeo manual con método privado `static {Entidad}Dto MapToDto({Entidad} e)`
- Sin AutoMapper, sin referencias a Infrastructure

## Paso 5 — Infrastructure: Repositorio

Crear `src/Infrastructure/Repositories/{Entidad}Repository.cs`:
- Implementa `I{Entidad}Repository`
- Primary constructor: `(AppDbContext context)`
- `GetAllAsync`: usar `.Include()` para navegaciones + `.AsNoTracking()`
- `GetByIdAsync`: usar `.Include()` + `.FirstOrDefaultAsync()`
- `CreateAsync` / `UpdateAsync`: guardar y re-fetch con `GetByIdAsync` para retornar con navegaciones cargadas
- `DeleteAsync`: SIEMPRE `entity.Activo = false` — nunca `context.Remove()`

## Paso 6 — Infrastructure: Registrar en AppDbContext y DI

**AppDbContext** (`src/Infrastructure/Persistence/AppDbContext.cs`):
- Agregar `public DbSet<{Entidad}> {Entidades} => Set<{Entidad}>();`
- Agregar global query filter: `modelBuilder.Entity<{Entidad}>().HasQueryFilter(e => e.Activo);`
- Configurar relaciones con `OnDelete` si hay riesgo de cascade paths

**DependencyInjection.cs**:
- `services.AddScoped<I{Entidad}Repository, {Entidad}Repository>();`
- `services.AddScoped<{Entidad}Service>();`

## Paso 7 — Infrastructure: Migración EF

```bash
dotnet ef migrations add Add{Entidad} --project src/Infrastructure --startup-project src/Server
dotnet ef database update --project src/Infrastructure --startup-project src/Server
```

## Paso 8 — Server: Controller

Crear `src/Server/Controllers/{Entidades}Controller.cs`:
- `[Authorize]`, `[ApiController]`, `[Route("api/v1/[controller]")]`
- Primary constructor: `({Entidad}Service service)`
- Endpoints: `GetAll`, `GetById`, `Create`, `Update`, `Delete`
- Retornos: `Ok()`, `CreatedAtAction()`, `NotFound()`, `NoContent()`
- Controlador delgado — sin lógica de negocio

## Paso 9 — Client: ApiService

Crear `src/Client/Services/{Entidad}ApiService.cs`:
- Primary constructor: `(HttpClient http)`
- `private const string Base = "api/v1/{entidades}";`
- Métodos con `GetFromJsonAsync`, `PostAsJsonAsync`, `PutAsJsonAsync`, `DeleteAsync`
- Registrar en `src/Client/Program.cs`: `builder.Services.AddScoped<{Entidad}ApiService>();`

## Paso 10 — Client: Página Blazor

Crear `src/Client/Pages/{Entidades}.razor`:
- `@page "/{entidades}"` + `@attribute [Authorize]`
- `@inject {Entidad}ApiService ApiService`
- `<Toast @ref="toast" />` + `<ConfirmDialog>` para eliminar
- Estados: `cargando`, `mostrarModal`, `guardando`, `confirmVisible`
- Secciones: loading spinner, empty-state, tabla con acciones
- Modal con `EditForm` + `DataAnnotationsValidator`
- Agregar NavLink en `src/Client/Layout/MainLayout.razor`

---

## Checklist final

- [ ] Entidad hereda BaseEntity
- [ ] DTOs en carpeta plural, namespace correcto
- [ ] Interfaz en Application/Interfaces
- [ ] Service sin referencias a Infrastructure
- [ ] Repositorio con soft delete (Activo = false)
- [ ] DbSet + global query filter en AppDbContext
- [ ] DI registrado (repositorio + servicio)
- [ ] Migración creada y aplicada
- [ ] Controller con [Authorize] y ruta api/v1/
- [ ] ApiService registrado en Client/Program.cs
- [ ] Página con [Authorize], Toast, ConfirmDialog, empty-state
- [ ] NavLink agregado en MainLayout
