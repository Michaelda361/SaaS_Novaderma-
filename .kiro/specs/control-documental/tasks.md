# Tareas de ImplementaciÃ³n â€” MÃ³dulo de Control Documental

## Tarea 1: Dominio â€” Entidades y Enums

- [x] Crear `src/Domain/Enums/DocumentoEnums.cs` con `TipoDocumento`, `EstadoDocumento`, `EstadoPropuesta`
- [x] Crear `src/Domain/Entities/Documento.cs` heredando `BaseEntity`
- [x] Crear `src/Domain/Entities/VersionDocumento.cs` (sin BaseEntity, inmutable)
- [x] Crear `src/Domain/Entities/PropuestaModificacion.cs` (sin BaseEntity)
- [x] Crear `src/Domain/Entities/FlujoAprobacion.cs` (sin BaseEntity, inmutable)

## Tarea 2: Shared â€” DTOs

- [x] Crear `src/Shared/DTOs/Documentos/DocumentoDto.cs`
- [x] Crear `src/Shared/DTOs/Documentos/DocumentoDetalleDto.cs`
- [x] Crear `src/Shared/DTOs/Documentos/CreateDocumentoDto.cs`
- [x] Crear `src/Shared/DTOs/Documentos/UpdateDocumentoDto.cs`
- [x] Crear `src/Shared/DTOs/Documentos/VersionDocumentoDto.cs`
- [x] Crear `src/Shared/DTOs/Documentos/FlujoAprobacionDto.cs`
- [x] Crear `src/Shared/DTOs/Documentos/PropuestaModificacionDto.cs`
- [x] Crear `src/Shared/DTOs/Documentos/CreatePropuestaDto.cs`
- [x] Crear `src/Shared/DTOs/Documentos/RechazarPropuestaDto.cs`

## Tarea 3: Application â€” Interfaces

- [x] Crear `src/Application/Interfaces/IDocumentoRepository.cs` con todos los mÃ©todos del diseÃ±o
- [x] Crear `src/Application/Interfaces/ISharePointService.cs` con los 5 mÃ©todos del diseÃ±o

## Tarea 4: Application â€” DocumentoService

- [x] Crear `src/Application/Services/DocumentoService.cs` con:
  - `ResolverColaboradorAsync` y `EsJefeDeAreaAsync`
  - `GetAllAsync` con filtros y lÃ³gica de rol
  - `GetByIdAsync` con lÃ³gica de visibilidad por rol
  - `CreateAsync`, `UpdateMetadatosAsync`, `DeleteAsync`
  - `SubirNuevaVersionAsync` (crea VersionDocumento, cambia estado si Publicado)
  - `AvanzarEstadoAsync` (valida transiciones, crea FlujoAprobacion)
  - `CrearPropuestaAsync` (valida Ã¡rea del colaborador, sube archivo temporal)
  - `GetPropuestasPendientesAsync`, `CountPropuestasPendientesAsync`
  - `AprobarPropuestaAsync` (transacciÃ³n: mover archivo, crear versiÃ³n, registrar flujo)
  - `RechazarPropuestaAsync` (transacciÃ³n: eliminar archivo temporal, registrar motivo)

## Tarea 5: Infrastructure â€” Paquetes NuGet

- [x] Agregar `Microsoft.Graph` al proyecto `src/Infrastructure`
- [x] Agregar `Azure.Identity` al proyecto `src/Infrastructure`

## Tarea 6: Infrastructure â€” SharePointService

- [x] Crear `src/Infrastructure/Services/SharePointService.cs` implementando `ISharePointService`:
  - AutenticaciÃ³n con `ClientSecretCredential` + `GraphServiceClient`
  - `SubirArchivoOficialAsync` â†’ carpeta `{BibliotecaDocumentos}/{TipoDocumento}/`
  - `SubirArchivoPropuestaAsync` â†’ carpeta `_propuestas-pendientes/{documentoId}/`
  - `MoverArchivoPropuestaAOficialAsync` â†’ mover item entre carpetas via Graph
  - `ObtenerUrlDescargaAsync` â†’ crear sharing link temporal (60 min)
  - `EliminarArchivoAsync` â†’ DELETE item en Graph
  - Capturar errores Graph y lanzar `SharePointException`
- [x] Crear `src/Infrastructure/Services/SharePointException.cs`

## Tarea 7: Infrastructure â€” DocumentoRepository

- [x] Crear `src/Infrastructure/Repositories/DocumentoRepository.cs` implementando `IDocumentoRepository`:
  - `GetAllAsync` con includes de Area
  - `GetPublicadosAsync` filtrando por Estado
  - `GetByIdConDetallesAsync` con includes de Versiones, Propuestas (con Colaborador), FlujoAprobacion
  - `GetPropuestasPendientesPorAreaAsync` y `CountPropuestasPendientesPorAreaAsync`
  - `CreatePropuestaAsync`, `UpdatePropuestaAsync`

## Tarea 8: Infrastructure â€” AppDbContext y MigraciÃ³n

- [x] Agregar `DbSet<Documento>`, `DbSet<VersionDocumento>`, `DbSet<PropuestaModificacion>`, `DbSet<FlujoAprobacion>` en `AppDbContext.cs`
- [x] Agregar configuraciÃ³n EF en `OnModelCreating`:
  - Soft delete filter para `Documento`
  - `DeleteBehavior.Restrict` en FK de `PropuestaModificacion` y `FlujoAprobacion` hacia `Colaborador`
  - ConversiÃ³n a string para todos los enums
- [x] Registrar `IDocumentoRepository`, `DocumentoService` e `ISharePointService` en `DependencyInjection.cs`
- [x] Ejecutar migraciÃ³n `AddControlDocumental`

## Tarea 9: Server â€” DocumentosController

- [x] Crear `src/Server/Controllers/DocumentosController.cs` con todos los endpoints del diseÃ±o
- [x] Implementar helper `GetColaboradorActualAsync()` para resolver identidad desde claim `preferred_username`
- [x] Leer configuraciÃ³n `SharePoint` desde `appsettings.json` y agregar secciÃ³n en `appsettings.json`

## Tarea 10: Client â€” DocumentoApiService

- [x] Crear `src/Client/Services/DocumentoApiService.cs` con todos los mÃ©todos del diseÃ±o
- [x] Usar `multipart/form-data` para endpoints de subida de archivo (`IBrowserFile` â†’ `StreamContent`)
- [x] Registrar `DocumentoApiService` como `Scoped` en `Client/Program.cs`
- [x] Agregar `@using TalentManagement.Shared.DTOs.Documentos` en `_Imports.razor`

## Tarea 11: Client â€” PÃ¡gina Documentos (`/documentos`)

- [x] Crear `src/Client/Pages/Documentos.razor` con:
  - Tabla con columnas: TÃ­tulo, Tipo, VersiÃ³n, Estado (badge), Ãrea, Acciones
  - Filtros por TipoDocumento, Estado (solo Admin), Ãrea
  - Estado vacÃ­o (`empty-state`) y loading state
  - Modal "Nuevo Documento" visible solo para Admin (campos + `InputFile`)
  - BotÃ³n "Ver detalle" â†’ navega a `/documentos/{id}`

## Tarea 12: Client â€” PÃ¡gina Detalle (`/documentos/{Id:int}`)

- [x] Crear `src/Client/Pages/DocumentoDetalle.razor` con:
  - Metadatos del documento
  - BotÃ³n "Descargar" (todos, solo si Publicado)
  - Botones de avance de estado (solo Admin)
  - Historial de versiones (lista)
  - LÃ­nea de tiempo del flujo de aprobaciÃ³n
  - SecciÃ³n "Propuestas" visible para Admin y JefeArea
  - BotÃ³n "Proponer Cambio" visible para Colaborador cuando Publicado â†’ modal con `Descripcion` + `InputFile` opcional
  - Redirect a `/documentos` + Toast si el documento no existe

## Tarea 13: Client â€” PÃ¡gina Propuestas (`/documentos/propuestas`)

- [x] Crear `src/Client/Pages/DocumentosPropuestas.razor` con:
  - Tabla: Documento, Propuesto por, DescripciÃ³n, Tiene archivo, Fecha, Acciones
  - BotÃ³n "Aprobar" â†’ `ConfirmDialog`
  - BotÃ³n "Rechazar" â†’ modal con campo `MotivoRechazo` obligatorio
  - Estado vacÃ­o si no hay propuestas pendientes
  - Toast de confirmaciÃ³n tras cada acciÃ³n

## Tarea 14: Client â€” Sidebar y badge de notificaciones

- [x] Agregar entrada "Documentos" ðŸ“„ en secciÃ³n "GestiÃ³n" de `MainLayout.razor`
- [x] Agregar `GetPageTitle()` case para `Documentos`, `DocumentoDetalle`, `DocumentosPropuestas`
- [x] Cargar conteo de propuestas pendientes al inicializar `MainLayout` y mostrar badge numÃ©rico
- [x] Decrementar badge al resolver propuestas desde `DocumentosPropuestas.razor`

## Tarea 15: Commit y push

- [x] Verificar compilaciÃ³n sin errores: `dotnet build`
- [x] Commit: `git add -A && git commit -m "feat: mÃ³dulo de control documental con flujo de aprobaciÃ³n y propuestas"`
- [x] Push: `git push origin main`
