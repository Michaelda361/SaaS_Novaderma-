# Diseño Técnico — Módulo de Control Documental

## Modelo de Datos

### Entidades de Dominio (`src/Domain/Entities/`)

```
Documento : BaseEntity
├── Titulo              string
├── TipoDocumento       enum TipoDocumento
├── Version             string          // "1.0", "1.1", "2.0"
├── Estado              enum EstadoDocumento
├── SharePointItemId    string
├── SharePointUrl       string
├── AreaId              int?
├── Area                Area?
├── Versiones           ICollection<VersionDocumento>
├── Propuestas          ICollection<PropuestaModificacion>
└── FlujoAprobacion     ICollection<FlujoAprobacion>

VersionDocumento  (no hereda BaseEntity — es inmutable)
├── Id                  int
├── DocumentoId         int
├── Documento           Documento
├── NumeroVersion       string
├── SharePointItemId    string
└── FechaCreacion       DateTime

PropuestaModificacion (no hereda BaseEntity — no se soft-delete)
├── Id                  int
├── DocumentoId         int
├── Documento           Documento
├── ColaboradorId       int
├── Colaborador         Colaborador
├── AreaId              int
├── Area                Area
├── Descripcion         string
├── SharePointItemIdPropuesta  string?   // archivo temporal, null si solo texto
├── EstadoPropuesta     enum EstadoPropuesta
├── AprobadorId         int?
├── Aprobador           Colaborador?
├── MotivoRechazo       string?
├── FechaCreacion       DateTime
└── FechaResolucion     DateTime?

FlujoAprobacion  (no hereda BaseEntity — es inmutable)
├── Id                  int
├── DocumentoId         int
├── Documento           Documento
├── EstadoAnterior      enum EstadoDocumento
├── EstadoNuevo         enum EstadoDocumento
├── ColaboradorId       int             // quien hizo la transición
├── Colaborador         Colaborador
└── FechaTransicion     DateTime
```

### Enums (`src/Domain/Enums/`)

```csharp
public enum TipoDocumento   { Politica, Procedimiento, Contrato, Manual, Reglamento }
public enum EstadoDocumento { Borrador, Revision, Aprobado, Publicado }
public enum EstadoPropuesta { PendienteRevision, Aprobada, Rechazada }
```

### Transiciones de estado permitidas

```
Documento:          Borrador → Revision → Aprobado → Publicado
PropuestaModificacion: PendienteRevision → Aprobada | Rechazada
```

---

## Estructura de Archivos

```
src/
├── Domain/
│   ├── Entities/
│   │   ├── Documento.cs
│   │   ├── VersionDocumento.cs
│   │   ├── PropuestaModificacion.cs
│   │   └── FlujoAprobacion.cs
│   └── Enums/
│       └── DocumentoEnums.cs
│
├── Application/
│   ├── Interfaces/
│   │   ├── IDocumentoRepository.cs
│   │   └── ISharePointService.cs
│   └── Services/
│       └── DocumentoService.cs
│
├── Infrastructure/
│   ├── Repositories/
│   │   └── DocumentoRepository.cs
│   ├── Services/
│   │   └── SharePointService.cs        // implementa ISharePointService
│   └── Persistence/
│       └── AppDbContext.cs             // agregar DbSets + config EF
│
├── Shared/
│   └── DTOs/
│       └── Documentos/
│           ├── DocumentoDto.cs
│           ├── DocumentoDetalleDto.cs
│           ├── CreateDocumentoDto.cs
│           ├── UpdateDocumentoDto.cs
│           ├── VersionDocumentoDto.cs
│           ├── FlujoAprobacionDto.cs
│           ├── PropuestaModificacionDto.cs
│           └── CreatePropuestaDto.cs
│
├── Server/
│   └── Controllers/
│       └── DocumentosController.cs
│
└── Client/
    ├── Pages/
    │   ├── Documentos.razor
    │   ├── DocumentoDetalle.razor
    │   └── DocumentosPropuestas.razor
    └── Services/
        └── DocumentoApiService.cs
```

---

## Interfaces de Application

### `IDocumentoRepository`

```csharp
Task<IEnumerable<Documento>> GetAllAsync();
Task<IEnumerable<Documento>> GetPublicadosAsync();
Task<Documento?> GetByIdAsync(int id);
Task<Documento?> GetByIdConDetallesAsync(int id);   // includes Versiones, Propuestas, Flujo
Task<Documento> CreateAsync(Documento documento);
Task<Documento> UpdateAsync(Documento documento);
Task DeleteAsync(int id);                            // soft delete
Task<IEnumerable<PropuestaModificacion>> GetPropuestasPendientesPorAreaAsync(int areaId);
Task<int> CountPropuestasPendientesPorAreaAsync(int areaId);
Task<PropuestaModificacion?> GetPropuestaByIdAsync(int id);
Task<PropuestaModificacion> CreatePropuestaAsync(PropuestaModificacion propuesta);
Task<PropuestaModificacion> UpdatePropuestaAsync(PropuestaModificacion propuesta);
```

### `ISharePointService`

```csharp
Task<(string itemId, string url)> SubirArchivoOficialAsync(
    Stream contenido, string nombreArchivo, TipoDocumento tipo);

Task<(string itemId, string url)> SubirArchivoPropuestaAsync(
    Stream contenido, string nombreArchivo, int documentoId);

Task MoverArchivoPropuestaAOficialAsync(
    string itemIdPropuesta, string nombreArchivo, TipoDocumento tipo);

Task<string> ObtenerUrlDescargaAsync(string itemId);   // URL temporal 60 min

Task EliminarArchivoAsync(string itemId);
```

---

## DocumentoService — Métodos principales

```csharp
// Listado
Task<IEnumerable<DocumentoDto>> GetAllAsync(FiltroDocumentoDto filtro, bool esAdmin);
Task<DocumentoDetalleDto?> GetByIdAsync(int id, bool esAdmin);

// CRUD Admin
Task<DocumentoDto> CreateAsync(CreateDocumentoDto dto, Stream archivo, string nombreArchivo);
Task<DocumentoDto?> UpdateMetadatosAsync(int id, UpdateDocumentoDto dto);
Task<bool> DeleteAsync(int id);
Task<DocumentoDto?> SubirNuevaVersionAsync(int id, Stream archivo, string nombreArchivo,
    bool incrementoMayor);

// Flujo de aprobación Admin
Task<DocumentoDto?> AvanzarEstadoAsync(int id, string emailUsuario);

// Resolución de identidad
Task<Colaborador> ResolverColaboradorAsync(string email);
Task<bool> EsJefeDeAreaAsync(int colaboradorId, out int areaId);

// Propuestas Colaborador
Task<PropuestaModificacionDto> CrearPropuestaAsync(int documentoId,
    CreatePropuestaDto dto, Stream? archivo, string? nombreArchivo, string emailUsuario);

// Propuestas JefeArea
Task<IEnumerable<PropuestaModificacionDto>> GetPropuestasPendientesAsync(string emailUsuario);
Task<int> CountPropuestasPendientesAsync(string emailUsuario);
Task AprobarPropuestaAsync(int propuestaId, string emailUsuario);
Task RechazarPropuestaAsync(int propuestaId, string motivoRechazo, string emailUsuario);
```

---

## DocumentosController — Endpoints

| Método | Ruta | Rol | Descripción |
|--------|------|-----|-------------|
| GET | `api/v1/documentos` | Todos | Listado con filtros query string |
| GET | `api/v1/documentos/{id}` | Todos | Detalle con versiones y flujo |
| POST | `api/v1/documentos` | Admin | Crear documento (multipart/form-data) |
| PUT | `api/v1/documentos/{id}/metadatos` | Admin | Actualizar título/tipo/área |
| DELETE | `api/v1/documentos/{id}` | Admin | Soft delete |
| POST | `api/v1/documentos/{id}/version` | Admin | Subir nueva versión (multipart) |
| POST | `api/v1/documentos/{id}/avanzar-estado` | Admin | Transición de estado |
| POST | `api/v1/documentos/{id}/propuestas` | Colaborador | Crear propuesta (multipart) |
| GET | `api/v1/documentos/propuestas/pendientes` | JefeArea | Propuestas pendientes del área |
| GET | `api/v1/documentos/propuestas/pendientes/count` | Todos | Conteo para badge sidebar |
| POST | `api/v1/documentos/propuestas/{id}/aprobar` | JefeArea | Aprobar propuesta |
| POST | `api/v1/documentos/propuestas/{id}/rechazar` | JefeArea | Rechazar con motivo |

### Resolución de rol en el controlador

```csharp
// Helper privado — se llama en cada acción que requiere identidad
private async Task<Colaborador> GetColaboradorActualAsync()
{
    var email = User.FindFirstValue("preferred_username")
             ?? User.FindFirstValue(ClaimTypes.Email)
             ?? throw new UnauthorizedAccessException();
    return await documentoService.ResolverColaboradorAsync(email);
}

// JefeArea se verifica en el servicio comparando colaborador.Id con Area.JefeId
```

---

## Configuración SharePoint (`appsettings.json`)

```json
"SharePoint": {
  "TenantId": "...",
  "ClientId": "...",
  "ClientSecret": "...",
  "SiteUrl": "https://{tenant}.sharepoint.com/sites/{sitio}",
  "BibliotecaDocumentos": "Documentos"
}
```

### Estructura de carpetas en SharePoint

```
Documentos/
├── Politica/
├── Procedimiento/
├── Contrato/
├── Manual/
├── Reglamento/
└── _propuestas-pendientes/
    └── {documentoId}/
```

---

## EF Core — Configuración en AppDbContext

```csharp
// DbSets nuevos
DbSet<Documento> Documentos
DbSet<VersionDocumento> VersionesDocumento
DbSet<PropuestaModificacion> PropuestasModificacion
DbSet<FlujoAprobacion> FlujosAprobacion

// Soft delete filter (solo Documento hereda BaseEntity)
modelBuilder.Entity<Documento>().HasQueryFilter(d => d.Activo);

// Relaciones sin cascade para evitar ciclos con Colaborador
modelBuilder.Entity<PropuestaModificacion>()
    .HasOne(p => p.Colaborador).WithMany()
    .HasForeignKey(p => p.ColaboradorId).OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<PropuestaModificacion>()
    .HasOne(p => p.Aprobador).WithMany()
    .HasForeignKey(p => p.AprobadorId).OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<FlujoAprobacion>()
    .HasOne(f => f.Colaborador).WithMany()
    .HasForeignKey(f => f.ColaboradorId).OnDelete(DeleteBehavior.Restrict);

// Precisión de enums almacenados como string para legibilidad
modelBuilder.Entity<Documento>()
    .Property(d => d.TipoDocumento).HasConversion<string>();
modelBuilder.Entity<Documento>()
    .Property(d => d.Estado).HasConversion<string>();
modelBuilder.Entity<PropuestaModificacion>()
    .Property(p => p.EstadoPropuesta).HasConversion<string>();
modelBuilder.Entity<FlujoAprobacion>()
    .Property(f => f.EstadoAnterior).HasConversion<string>();
modelBuilder.Entity<FlujoAprobacion>()
    .Property(f => f.EstadoNuevo).HasConversion<string>();
```

---

## DTOs principales (`Shared/DTOs/Documentos/`)

```csharp
// Lista
DocumentoDto { Id, Titulo, TipoDocumento, Version, Estado, AreaId, AreaNombre }

// Detalle completo
DocumentoDetalleDto : DocumentoDto {
    SharePointUrl,
    Versiones: List<VersionDocumentoDto>,
    Flujo: List<FlujoAprobacionDto>,
    Propuestas: List<PropuestaModificacionDto>   // solo visible para Admin/JefeArea
}

// Creación
CreateDocumentoDto { Titulo, TipoDocumento, AreaId? }   // archivo va en multipart

// Actualización de metadatos
UpdateDocumentoDto { Titulo, TipoDocumento, AreaId? }

// Propuesta
CreatePropuestaDto { Descripcion }   // archivo va en multipart
PropuestaModificacionDto {
    Id, DocumentoId, DocumentoTitulo,
    ColaboradorId, ColaboradorNombre,
    AreaId, AreaNombre,
    Descripcion, TieneArchivo,
    EstadoPropuesta, MotivoRechazo?,
    FechaCreacion, FechaResolucion?
}

// Rechazo
RechazarPropuestaDto { MotivoRechazo }
```

---

## Paquetes NuGet requeridos (`src/Infrastructure`)

```
Microsoft.Graph                    // Graph API SDK
Azure.Identity                     // ClientSecretCredential para Graph
```

---

## Migración EF

```bash
dotnet ef migrations add AddControlDocumental \
  --project src/Infrastructure \
  --startup-project src/Server
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Server
```

---

## Blazor Client — Páginas y servicios

### `DocumentoApiService`

```csharp
Task<List<DocumentoDto>> GetAllAsync(string? tipo, string? estado, int? areaId, string? busqueda);
Task<DocumentoDetalleDto?> GetByIdAsync(int id);
Task<DocumentoDto?> CreateAsync(CreateDocumentoDto dto, IBrowserFile archivo);
Task<DocumentoDto?> UpdateMetadatosAsync(int id, UpdateDocumentoDto dto);
Task DeleteAsync(int id);
Task<DocumentoDto?> SubirNuevaVersionAsync(int id, IBrowserFile archivo, bool incrementoMayor);
Task AvanzarEstadoAsync(int id);
Task<string?> ObtenerUrlDescargaAsync(int id);
Task CrearPropuestaAsync(int documentoId, CreatePropuestaDto dto, IBrowserFile? archivo);
Task<List<PropuestaModificacionDto>> GetPropuestasPendientesAsync();
Task<int> GetPropuestasPendientesCountAsync();
Task AprobarPropuestaAsync(int propuestaId);
Task RechazarPropuestaAsync(int propuestaId, string motivo);
```

### Páginas

| Página | Ruta | Descripción |
|--------|------|-------------|
| `Documentos.razor` | `/documentos` | Listado con filtros, modal de creación (Admin) |
| `DocumentoDetalle.razor` | `/documentos/{Id:int}` | Detalle, versiones, flujo, propuestas |
| `DocumentosPropuestas.razor` | `/documentos/propuestas` | Bandeja de propuestas (JefeArea) |
