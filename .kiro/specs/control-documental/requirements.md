# Documento de Requisitos — Módulo de Control Documental

## Introducción

El Módulo de Control Documental permite a NovaHub gestionar documentos organizacionales
(Políticas, Procedimientos, Contratos, Manuales y Reglamentos) almacenados en SharePoint
a través de Microsoft Graph API. Los metadatos, el estado del flujo de aprobación y las
propuestas de modificación se persisten en SQL Server. Los administradores gestionan el
ciclo de vida completo; los colaboradores pueden consultar, descargar y proponer cambios
a documentos publicados; los jefes de área revisan y resuelven dichas propuestas antes de
que los cambios se incorporen al documento oficial.

---

## Glosario

- **Documento**: Entidad de dominio que representa un documento organizacional. Almacena
  metadatos en SQL Server y el archivo físico en SharePoint.
- **VersionDocumento**: Registro histórico de cada versión aprobada de un Documento.
- **FlujoAprobacion**: Registro de cada transición de estado de un Documento, incluyendo
  el responsable y la fecha.
- **PropuestaModificacion**: Entidad que representa un cambio propuesto por un Colaborador
  sobre un Documento publicado. Tiene su propio ciclo de vida independiente del Documento.
- **ISharePointService**: Interfaz en la capa Application que abstrae las operaciones con
  Microsoft Graph API (subir, descargar, eliminar archivos).
- **DocumentoService**: Servicio de aplicación que orquesta la lógica de negocio del módulo.
- **DocumentosController**: Controlador ASP.NET Core que expone los endpoints REST.
- **Documento_UI**: Páginas Blazor WASM del módulo.
- **Admin**: Usuario con rol `Admin` en Azure Entra ID.
- **Colaborador_Usuario**: Usuario con rol `Colaborador` en Azure Entra ID.
- **JefeArea**: Colaborador cuyo `Id` coincide con `Area.JefeId`. No es un rol de Azure AD;
  el servidor lo determina comparando el claim de identidad del token con la tabla `Areas`.
- **IdentidadColaborador**: Relación entre el usuario autenticado (claim `preferred_username`
  o `oid` del token JWT) y su registro `Colaborador` en la base de datos. El servidor
  resuelve esta relación en cada request mediante el email del claim.
- **Estado**: Ciclo de vida de un Documento: `Borrador → Revisión → Aprobado → Publicado`.
- **EstadoPropuesta**: Ciclo de vida de una PropuestaModificacion:
  `PendienteRevision → Aprobada | Rechazada`.
- **TipoDocumento**: Categoría del documento: `Política`, `Procedimiento`, `Contrato`,
  `Manual`, `Reglamento`.

---

## Requisitos

### Requisito 1: Gestión de documentos por el administrador

**User Story:** Como Admin, quiero crear, editar y eliminar documentos con sus metadatos,
para mantener el repositorio documental actualizado.

#### Criterios de Aceptación

1. WHEN un Admin envía una solicitud de creación con `Titulo`, `TipoDocumento` y un archivo
   adjunto válido, THE DocumentoService SHALL crear un registro `Documento` con
   `Estado = Borrador`, subir el archivo a SharePoint en la carpeta correspondiente al
   `TipoDocumento` y persistir el `SharePointItemId` y `SharePointUrl` en SQL Server.

2. WHEN un Admin envía una solicitud de actualización de metadatos (`Titulo`, `TipoDocumento`,
   `AreaId`), THE DocumentoService SHALL actualizar únicamente esos campos sin modificar
   el `Estado`, la versión ni el historial.

3. WHEN un Admin solicita eliminar un Documento, THE DocumentoService SHALL establecer
   `Activo = false` (soft delete) sin eliminar el archivo en SharePoint.

4. IF un Admin envía una solicitud de creación con `Titulo` vacío o sin archivo adjunto,
   THEN THE DocumentosController SHALL retornar HTTP 400 con mensaje descriptivo.

5. IF el ISharePointService no puede subir el archivo, THEN THE DocumentoService SHALL
   revertir la creación del registro SQL y propagar el error al controlador.

---

### Requisito 2: Versionado de documentos

**User Story:** Como Admin, quiero subir nuevas versiones de un documento existente,
para mantener el historial de cambios sin perder versiones anteriores.

#### Criterios de Aceptación

1. WHEN un Admin sube un nuevo archivo sobre un Documento existente, THE DocumentoService
   SHALL crear un registro `VersionDocumento` con el `SharePointItemId` anterior, el número
   de versión anterior y la fecha, antes de actualizar el Documento con el nuevo
   `SharePointItemId` e incrementar la versión.

2. THE DocumentoService SHALL usar formato de versión `Mayor.Menor` (ej: `1.0`, `1.1`,
   `2.0`). El Admin especifica si el incremento es mayor o menor al subir la nueva versión.

3. WHEN se solicita el historial de versiones, THE DocumentoService SHALL retornar todos
   los registros `VersionDocumento` ordenados de más reciente a más antiguo.

4. IF un Admin sube una nueva versión sobre un Documento con `Estado = Publicado`, THEN
   THE DocumentoService SHALL cambiar el `Estado` a `Borrador` automáticamente antes de
   registrar la nueva versión, requiriendo que pase nuevamente por el flujo de aprobación.

---

### Requisito 3: Flujo de aprobación (Admin)

**User Story:** Como Admin, quiero gestionar el flujo de aprobación de los documentos,
para garantizar que solo los documentos revisados y aprobados sean publicados.

#### Criterios de Aceptación

1. WHEN un Admin solicita avanzar el estado de un Documento, THE DocumentoService SHALL
   permitir únicamente las transiciones: `Borrador → Revisión`, `Revisión → Aprobado`,
   `Aprobado → Publicado`.

2. WHEN se realiza una transición de estado, THE DocumentoService SHALL crear un registro
   `FlujoAprobacion` con `EstadoAnterior`, `EstadoNuevo`, el `ColaboradorId` resuelto del
   usuario autenticado y la fecha UTC.

3. IF un Admin intenta una transición no permitida (ej: `Borrador → Publicado`), THEN THE
   DocumentosController SHALL retornar HTTP 422 indicando las transiciones válidas desde
   el estado actual.

4. WHILE un Documento tiene `Estado = Publicado`, THE DocumentoService SHALL impedir
   modificaciones de metadatos o reemplazo de archivo hasta que un Admin inicie una nueva
   versión (lo que cambia el estado a `Borrador` según Requisito 2.4).

---

### Requisito 4: Resolución de identidad del colaborador autenticado

**User Story:** Como sistema, necesito saber qué registro `Colaborador` corresponde al
usuario autenticado, para aplicar correctamente las reglas de negocio de propuestas y
aprobaciones.

#### Criterios de Aceptación

1. THE DocumentosController SHALL resolver el `Colaborador` del usuario autenticado
   buscando en la tabla `Colaboradores` por el claim `preferred_username` (email) del
   token JWT en cada operación que requiera identidad (proponer, aprobar, rechazar).

2. IF no existe un `Colaborador` con el email del token, THEN THE DocumentosController
   SHALL retornar HTTP 403 con mensaje "Usuario no registrado como colaborador".

3. THE DocumentoService SHALL exponer un método `ResolverColaboradorAsync(string email)`
   que retorna el `Colaborador` o lanza una excepción tipada si no existe.

---

### Requisito 5: Consulta y descarga

**User Story:** Como usuario autenticado, quiero consultar y descargar documentos
publicados para acceder a las políticas y procedimientos vigentes.

#### Criterios de Aceptación

1. WHEN un Colaborador_Usuario solicita la lista de documentos, THE DocumentosController
   SHALL retornar únicamente documentos con `Estado = Publicado` y `Activo = true`.

2. WHEN un Admin solicita la lista de documentos, THE DocumentosController SHALL retornar
   todos los documentos con `Activo = true` independientemente del estado.

3. WHEN cualquier usuario autenticado solicita la descarga de un Documento publicado, THE
   DocumentoService SHALL obtener una URL de descarga temporal de SharePoint (válida
   mínimo 60 minutos) y retornarla al cliente para descarga directa.

4. IF un Colaborador_Usuario intenta acceder al detalle de un Documento con `Estado`
   distinto de `Publicado`, THEN THE DocumentosController SHALL retornar HTTP 403.

---

### Requisito 6: Búsqueda y filtrado

**User Story:** Como usuario autenticado, quiero filtrar y buscar documentos para
encontrar rápidamente lo que necesito.

#### Criterios de Aceptación

1. THE DocumentosController SHALL aceptar los parámetros de query string opcionales:
   `tipoDocumento`, `areaId`, `estado` y `busqueda`.

2. WHEN se proporciona `busqueda`, THE DocumentoService SHALL filtrar documentos cuyo
   `Titulo` contenga el texto (insensible a mayúsculas/minúsculas).

3. WHEN se proporcionan múltiples parámetros, THE DocumentoService SHALL aplicarlos
   combinados (AND lógico).

4. El parámetro `estado` solo es visible para Admins; si un Colaborador_Usuario lo envía,
   THE DocumentosController SHALL ignorarlo y aplicar siempre `Estado = Publicado`.

---

### Requisito 7: Integración con SharePoint vía Microsoft Graph API

**User Story:** Como sistema, quiero almacenar archivos en SharePoint usando la licencia
M365 existente, sin costos adicionales de almacenamiento.

#### Criterios de Aceptación

1. THE ISharePointService SHALL autenticarse con Microsoft Graph API usando client
   credentials flow con `TenantId`, `ClientId` y `ClientSecret` configurados en
   `appsettings.json` bajo la sección `SharePoint`.

2. WHEN se sube un archivo de documento aprobado, THE ISharePointService SHALL almacenarlo
   en la biblioteca configurada en `SharePoint:SiteUrl`, en una subcarpeta con el nombre
   del `TipoDocumento`.

3. WHEN se sube un archivo de propuesta pendiente, THE ISharePointService SHALL
   almacenarlo en una subcarpeta `_propuestas-pendientes/{documentoId}/` separada de los
   documentos oficiales.

4. WHEN se rechaza una propuesta que tenía archivo adjunto, THE DocumentoService SHALL
   llamar a `ISharePointService.EliminarArchivoAsync` para limpiar el archivo temporal
   de SharePoint, evitando archivos huérfanos.

5. WHEN se aprueba una propuesta que tenía archivo adjunto, THE DocumentoService SHALL
   mover el archivo desde `_propuestas-pendientes/` a la carpeta oficial del tipo de
   documento antes de actualizar el `SharePointItemId` del Documento.

6. IF Microsoft Graph API retorna error HTTP 4xx o 5xx, THEN THE ISharePointService SHALL
   lanzar una excepción tipada `SharePointException` con el código y mensaje original.

7. THE ISharePointService SHALL definirse en la capa `Application/Interfaces/` para que
   `DocumentoService` no dependa de la implementación de infraestructura.

---

### Requisito 8: Propuestas de modificación por colaboradores

**User Story:** Como Colaborador_Usuario, quiero proponer cambios a un documento publicado
para contribuir a mantener la documentación actualizada.

#### Criterios de Aceptación

1. Una `PropuestaModificacion` consiste únicamente en: una `Descripcion` del cambio
   (obligatoria) y opcionalmente un archivo nuevo que reemplazaría al actual. Los
   colaboradores NO pueden modificar `Titulo`, `TipoDocumento` ni `AreaId` directamente.

2. WHEN un Colaborador_Usuario envía una propuesta sobre un Documento con
   `Estado = Publicado`, THE DocumentoService SHALL crear el registro `PropuestaModificacion`
   con `EstadoPropuesta = PendienteRevision`, el `ColaboradorId` del proponente resuelto
   según Requisito 4, y si hay archivo adjunto, subirlo a la carpeta temporal de SharePoint
   (Requisito 7.3) y guardar el `SharePointItemIdPropuesta`.

3. THE DocumentoService SHALL asociar la propuesta al `AreaId` del Colaborador proponente
   para que el JefeArea correspondiente pueda revisarla.

4. IF el Colaborador_Usuario no tiene `AreaId` asignado, THEN THE DocumentosController
   SHALL retornar HTTP 422 con mensaje "Debes estar asignado a un área para proponer
   modificaciones".

5. IF el Documento tiene `Estado` distinto de `Publicado`, THEN THE DocumentosController
   SHALL retornar HTTP 422 con mensaje "Solo se pueden proponer cambios sobre documentos
   publicados".

6. WHEN se crea la propuesta, THE DocumentoService SHALL registrar `FechaCreacion` en UTC.

---

### Requisito 9: Revisión y aprobación de propuestas por el jefe de área

**User Story:** Como JefeArea, quiero revisar y resolver las propuestas de modificación
de los colaboradores de mi área para controlar qué cambios se incorporan a los documentos
oficiales.

#### Criterios de Aceptación

1. THE DocumentosController SHALL determinar si el usuario autenticado es JefeArea
   verificando que su `ColaboradorId` (resuelto según Requisito 4) coincida con
   `Area.JefeId` de algún área activa.

2. WHEN un JefeArea solicita sus propuestas pendientes, THE DocumentoService SHALL
   retornar únicamente las `PropuestaModificacion` con `EstadoPropuesta = PendienteRevision`
   cuyo `AreaId` coincida con el área que dirige.

3. IF el área del Documento no tiene `JefeId` asignado, THEN THE DocumentoService SHALL
   hacer visibles las propuestas de ese documento a todos los Admins como fallback.

4. WHEN un JefeArea aprueba una propuesta, THE DocumentoService SHALL en una sola
   transacción:
   a. Cambiar `EstadoPropuesta` a `Aprobada`, registrar `AprobadorId` y `FechaResolucion` UTC.
   b. Si la propuesta tiene archivo: mover el archivo temporal a la carpeta oficial
      (Requisito 7.5), crear un `VersionDocumento` con incremento de versión menor y
      actualizar `SharePointItemId` del Documento.
   c. Si la propuesta es solo descripción: no cambiar la versión ni el archivo.
   d. Mantener `Estado = Publicado` en el Documento tras la aprobación.
   e. Registrar la transición en `FlujoAprobacion` (Requisito 3.2).

5. WHEN un JefeArea rechaza una propuesta, THE DocumentoService SHALL en una sola
   transacción:
   a. Cambiar `EstadoPropuesta` a `Rechazada`, registrar `AprobadorId`, `FechaResolucion`
      UTC y `MotivoRechazo`.
   b. Si la propuesta tenía archivo adjunto, eliminarlo de SharePoint (Requisito 7.4).

6. IF el usuario autenticado no es JefeArea del área correspondiente, THEN THE
   DocumentosController SHALL retornar HTTP 403.

7. IF la propuesta ya fue resuelta (`EstadoPropuesta != PendienteRevision`), THEN THE
   DocumentosController SHALL retornar HTTP 422.

---

### Requisito 10: Notificaciones internas

**User Story:** Como JefeArea, quiero saber cuándo llegan propuestas pendientes sin tener
que revisar manualmente la página de propuestas.

#### Criterios de Aceptación

1. THE Documento_UI SHALL mostrar en el sidebar un badge numérico junto a la entrada
   "Documentos" con el conteo de propuestas `PendienteRevision` del área del usuario,
   actualizado al cargar la aplicación.

2. THE DocumentosController SHALL exponer un endpoint `GET api/v1/documentos/propuestas/pendientes/count`
   que retorna el conteo de propuestas pendientes para el JefeArea autenticado (0 si el
   usuario no es JefeArea).

3. WHEN se resuelve una propuesta, THE Documento_UI SHALL decrementar el badge
   automáticamente sin recargar la página.

4. (Opcional — fase 2) WHEN se crea una `PropuestaModificacion`, THE DocumentoService
   SHOULD enviar un email de notificación al JefeArea usando Microsoft Graph API
   (`POST /users/{jefeEmail}/sendMail`) aprovechando la licencia M365 existente.

---

### Requisito 11: Interfaz de usuario — Listado de documentos (`/documentos`)

**User Story:** Como usuario autenticado, quiero una página de listado con filtros
visuales para navegar el repositorio documental eficientemente.

#### Criterios de Aceptación

1. THE Documento_UI SHALL mostrar una tabla con columnas: `Título`, `Tipo`, `Versión`,
   `Estado`, `Área` y acciones según el rol del usuario.

2. THE Documento_UI SHALL mostrar controles de filtro por `TipoDocumento`, `Estado` (solo
   Admin) y `Área` que actualicen la tabla sin recargar la página.

3. THE Documento_UI SHALL mostrar el `Estado` como badge con color: Borrador (gris),
   Revisión (amarillo), Aprobado (azul), Publicado (verde).

4. WHILE carga datos, THE Documento_UI SHALL mostrar un indicador de carga.

5. IF no hay documentos, THE Documento_UI SHALL mostrar un `empty-state` descriptivo.

6. WHERE el usuario es Admin, THE Documento_UI SHALL mostrar un botón "Nuevo Documento"
   que abre un modal con campos: `Título`, `Tipo`, `Área` (opcional) y selector de archivo.

---

### Requisito 12: Interfaz de usuario — Detalle de documento (`/documentos/{id}`)

**User Story:** Como usuario autenticado, quiero ver el detalle completo de un documento
con historial de versiones, flujo de aprobación y propuestas.

#### Criterios de Aceptación

1. THE Documento_UI SHALL mostrar: `Título`, `Tipo`, `Versión actual`, `Estado`, `Área` y
   `Colaborador` asociado (si aplica).

2. THE Documento_UI SHALL mostrar el historial de versiones ordenado de más reciente a
   más antiguo con número de versión y fecha.

3. THE Documento_UI SHALL mostrar el flujo de aprobación como línea de tiempo con estado
   anterior, estado nuevo, responsable y fecha de cada transición.

4. WHERE el usuario es Admin, THE Documento_UI SHALL mostrar botones para avanzar el
   estado según las transiciones del Requisito 3.1.

5. THE Documento_UI SHALL mostrar un botón "Descargar" para todos los usuarios cuando
   `Estado = Publicado`.

6. WHERE el usuario es Colaborador_Usuario y `Estado = Publicado`, THE Documento_UI SHALL
   mostrar el botón "Proponer Cambio" que abre un modal con campo `Descripcion`
   (obligatorio) y selector de archivo (opcional).

7. WHERE el usuario es Admin o JefeArea, THE Documento_UI SHALL mostrar una sección
   "Propuestas" con la lista de todas las `PropuestaModificacion` del documento, su
   estado, el colaborador proponente, la fecha y el motivo de rechazo si aplica.

8. IF el `id` no existe o `Activo = false`, THE Documento_UI SHALL redirigir a `/documentos`
   y mostrar un error via `Toast`.

---

### Requisito 13: Interfaz de usuario — Propuestas pendientes (`/documentos/propuestas`)

**User Story:** Como JefeArea, quiero una página dedicada para gestionar todas las
propuestas pendientes de mi área.

#### Criterios de Aceptación

1. THE Documento_UI SHALL mostrar una tabla con: `Documento`, `Propuesto por`,
   `Descripción`, `Tiene archivo`, `Fecha` y botones "Aprobar" / "Rechazar".

2. WHEN el JefeArea hace clic en "Aprobar", THE Documento_UI SHALL mostrar un
   `ConfirmDialog` antes de enviar la solicitud.

3. WHEN el JefeArea hace clic en "Rechazar", THE Documento_UI SHALL mostrar un modal con
   campo de texto obligatorio `MotivoRechazo` antes de enviar la solicitud.

4. WHEN se resuelve una propuesta, THE Documento_UI SHALL actualizar la lista y mostrar
   confirmación via `Toast`.

5. IF no hay propuestas pendientes, THE Documento_UI SHALL mostrar un `empty-state`
   con mensaje "No hay propuestas pendientes en tu área".

---

### Requisito 14: Navegación y registro de servicios

**User Story:** Como usuario autenticado, quiero acceder al módulo desde el menú lateral
integrado con el resto de la aplicación.

#### Criterios de Aceptación

1. THE Documento_UI SHALL agregar la entrada "Documentos" con ícono 📄 en la sección
   "Gestión" del sidebar de `MainLayout.razor`, con enlace a `/documentos` y badge de
   propuestas pendientes (Requisito 10.1).

2. THE Documento_UI SHALL registrar las rutas `@page "/documentos"`,
   `@page "/documentos/{id:int}"` y `@page "/documentos/propuestas"`.

3. THE Documento_UI SHALL agregar `@using TalentManagement.Shared.DTOs.Documentos` en
   `_Imports.razor`.

4. THE Documento_UI SHALL registrar `DocumentoApiService` como `Scoped` en
   `Client/Program.cs`.

5. THE DocumentoService, `IDocumentoRepository` y `ISharePointService` SHALL registrarse
   en `Infrastructure/DependencyInjection.cs`.
