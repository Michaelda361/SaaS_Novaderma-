# Tareas de Implementación — Módulo de Control Documental

## Tarea 1: Dominio — Entidades y Enums

- [ ] Crear `src/Domain/Enums/DocumentoEnums.cs` con `TipoDocumento`, `EstadoDocumento`, `EstadoPropuesta`
- [ ] Crear `src/Domain/Entities/Documento.cs` heredando `BaseEntity`
- [ ] Crear `src/Domain/Entities/VersionDocumento.cs` (sin BaseEntity, inmutable)
- [ ] Crear `src/Domain/Entities/PropuestaModificacion.cs` (sin BaseEntity)
- [ ] Crear `src/Domain/Entities/FlujoAprobacion.cs` (sin BaseEntity, inmutable)

## Tarea 2: Shared — DTOs

- [ ] Crear `src/Shared/DTOs/Documentos/DocumentoDto.cs`
- [ ] Crear `src/Shared/DTOs/Documentos/DocumentoDetalleDto.cs`
- [ ] Crear `src/Shared/DTOs/Documentos/CreateDocumentoDto.cs`
- [ ] Crear `src/Shared/DTOs/Documentos/UpdateDocumentoDto.cs`
- [ ] Crear `src/Shared/DTOs/Documentos/VersionDocumentoDto.cs`
- [ ] Crear `src/Shared/DTOs/Documentos/FlujoAprobacionDto.cs`
- [ ] Crear `src/Shared/DTOs/Documentos/PropuestaModificacionDto.cs`
- [ ] Crear `src/Shared/DTOs/Documentos/CreatePropuestaDto.cs`
- [ ] Crear `src/Shared/DTOs/Documentos/RechazarPropuestaDto.cs`

## Tarea 3: Application — Interfaces

- [ ] Crear `src/Application/Interfaces/IDocumentoRepository.cs` con todos los métodos del diseño
- [ ] Crear `src/Application/Interfaces/ISharePointService.cs` con los 5 métodos del diseño

## Tarea 4: Application — DocumentoService

- [ ] Crear `src/Application/Services/DocumentoService.cs` con:
  - `ResolverColaboradorAsync` y `EsJefeDeAreaAsync`
  - `GetAllAsync` con filtros y lógica de rol
  - `GetByIdAsync` con lógica de visibilidad por rol
  - `CreateAsync`, `UpdateMetadatosAsync`, `DeleteAsync`
  - `SubirNuevaVersionAsync` (crea VersionDocumento, cambia estado si Publicado)
  - `AvanzarEstadoAsync` (valida transiciones, crea FlujoAprobacion)
  - `CrearPropuestaAsync` (valida área del colaborador, sube archivo temporal)
  - `GetPropuestasPendientesAsync`, `CountPropuestasPendientesAsync`
  - `AprobarPropuestaAsync` (transacción: mover archivo, crear versión, registrar flujo)
  - `RechazarPropuestaAsync` (transacción: eliminar archivo temporal, registrar motivo)

## Tarea 5: Infrastructure — Paquetes NuGet

- [ ] Agregar `Microsoft.Graph` al proyecto `src/Infrastructure`
- [ ] Agregar `Azure.Identity` al proyecto `src/Infrastructure`

## Tarea 6: Infrastructure — SharePointService

- [ ] Crear `src/Infrastructure/Services/SharePointService.cs` implementando `ISharePointService`:
  - Autenticación con `ClientSecretCredential` + `GraphServiceClient`
  - `SubirArchivoOficialAsync` → carpeta `{BibliotecaDocumentos}/{TipoDocumento}/`
  - `SubirArchivoPropuestaAsync` → carpeta `_propuestas-pendientes/{documentoId}/`
  - `MoverArchivoPropuestaAOficialAsync` → mover item entre carpetas via Graph
  - `ObtenerUrlDescargaAsync` → crear sharing link temporal (60 min)
  - `EliminarArchivoAsync` → DELETE item en Graph
  - Capturar errores Graph y lanzar `SharePointException`
- [ ] Crear `src/Infrastructure/Services/SharePointException.cs`

## Tarea 7: Infrastructure — DocumentoRepository

- [ ] Crear `src/Infrastructure/Repositories/DocumentoRepository.cs` implementando `IDocumentoRepository`:
  - `GetAllAsync` con includes de Area
  - `GetPublicadosAsync` filtrando por Estado
  - `GetByIdConDetallesAsync` con includes de Versiones, Propuestas (con Colaborador), FlujoAprobacion
  - `GetPropuestasPendientesPorAreaAsync` y `CountPropuestasPendientesPorAreaAsync`
  - `CreatePropuestaAsync`, `UpdatePropuestaAsync`

## Tarea 8: Infrastructure — AppDbContext y Migración

- [ ] Agregar `DbSet<Documento>`, `DbSet<VersionDocumento>`, `DbSet<PropuestaModificacion>`, `DbSet<FlujoAprobacion>` en `AppDbContext.cs`
- [ ] Agregar configuración EF en `OnModelCreating`:
  - Soft delete filter para `Documento`
  - `DeleteBehavior.Restrict` en FK de `PropuestaModificacion` y `FlujoAprobacion` hacia `Colaborador`
  - Conversión a string para todos los enums
- [ ] Registrar `IDocumentoRepository`, `DocumentoService` e `ISharePointService` en `DependencyInjection.cs`
- [ ] Ejecutar migración `AddControlDocumental`

## Tarea 9: Server — DocumentosController

- [ ] Crear `src/Server/Controllers/DocumentosController.cs` con todos los endpoints del diseño
- [ ] Implementar helper `GetColaboradorActualAsync()` para resolver identidad desde claim `preferred_username`
- [ ] Leer configuración `SharePoint` desde `appsettings.json` y agregar sección en `appsettings.json`

## Tarea 10: Client — DocumentoApiService

- [ ] Crear `src/Client/Services/DocumentoApiService.cs` con todos los métodos del diseño
- [ ] Usar `multipart/form-data` para endpoints de subida de archivo (`IBrowserFile` → `StreamContent`)
- [ ] Registrar `DocumentoApiService` como `Scoped` en `Client/Program.cs`
- [ ] Agregar `@using TalentManagement.Shared.DTOs.Documentos` en `_Imports.razor`

## Tarea 11: Client — Página Documentos (`/documentos`)

- [ ] Crear `src/Client/Pages/Documentos.razor` con:
  - Tabla con columnas: Título, Tipo, Versión, Estado (badge), Área, Acciones
  - Filtros por TipoDocumento, Estado (solo Admin), Área
  - Estado vacío (`empty-state`) y loading state
  - Modal "Nuevo Documento" visible solo para Admin (campos + `InputFile`)
  - Botón "Ver detalle" → navega a `/documentos/{id}`

## Tarea 12: Client — Página Detalle (`/documentos/{Id:int}`)

- [ ] Crear `src/Client/Pages/DocumentoDetalle.razor` con:
  - Metadatos del documento
  - Botón "Descargar" (todos, solo si Publicado)
  - Botones de avance de estado (solo Admin)
  - Historial de versiones (lista)
  - Línea de tiempo del flujo de aprobación
  - Sección "Propuestas" visible para Admin y JefeArea
  - Botón "Proponer Cambio" visible para Colaborador cuando Publicado → modal con `Descripcion` + `InputFile` opcional
  - Redirect a `/documentos` + Toast si el documento no existe

## Tarea 13: Client — Página Propuestas (`/documentos/propuestas`)

- [ ] Crear `src/Client/Pages/DocumentosPropuestas.razor` con:
  - Tabla: Documento, Propuesto por, Descripción, Tiene archivo, Fecha, Acciones
  - Botón "Aprobar" → `ConfirmDialog`
  - Botón "Rechazar" → modal con campo `MotivoRechazo` obligatorio
  - Estado vacío si no hay propuestas pendientes
  - Toast de confirmación tras cada acción

## Tarea 14: Client — Sidebar y badge de notificaciones

- [ ] Agregar entrada "Documentos" 📄 en sección "Gestión" de `MainLayout.razor`
- [ ] Agregar `GetPageTitle()` case para `Documentos`, `DocumentoDetalle`, `DocumentosPropuestas`
- [ ] Cargar conteo de propuestas pendientes al inicializar `MainLayout` y mostrar badge numérico
- [ ] Decrementar badge al resolver propuestas desde `DocumentosPropuestas.razor`

## Tarea 15: Commit y push

- [ ] Verificar compilación sin errores: `dotnet build`
- [ ] Commit: `git add -A && git commit -m "feat: módulo de control documental con flujo de aprobación y propuestas"`
- [ ] Push: `git push origin main`
