---
inclusion: always
---

# Entidades del Dominio — NovaHub

Catálogo completo de entidades existentes. Antes de proponer una nueva entidad, verificar que no exista ya aquí.

## Núcleo de Talento

### Colaborador
Empleado de la organización.
- `Nombre`, `Apellido`, `Email`, `Telefono`, `FechaIngreso`
- `Cedula?`, `TipoContrato?`, `SueldoBasico?` (decimal), `Ciudad?`
- `Rol` (enum `RolUsuario`: Colaborador / Jefe / Admin) — almacenado como string, default `Colaborador`
- FK: `AreaId` → Area, `CargoId` → Cargo, `SupervisorId?` → Colaborador (auto-referencia)
- Colecciones: `Certificados`, `Inscripciones`

### Area
Unidad organizacional (departamento).
- `Nombre`, `Descripcion`
- FK: `JefeId?` → Colaborador (OnDelete: SetNull)
- Colecciones: `Colaboradores`, `Cargos`

### Cargo
Puesto de trabajo, siempre scoped a un área.
- `Nombre`, `Descripcion`
- FK: `AreaId` → Area
- Colecciones: `Colaboradores`

---

## Capacitaciones

### Capacitacion
Sesión de entrenamiento. Puede ser general, por área o por colaborador.
- `Nombre`, `Descripcion`, `DuracionHoras` (int), `FechaInicio`, `FechaFin`
- FK opcionales: `AreaId?` → Area, `ColaboradorId?` → Colaborador
- Colecciones: `Inscripciones`, `Recursos`
- Lógica: `TipoAsignacion` = "General" | "Área" | "Colaborador" (calculado en DTO)

### Inscripcion
Vincula un Colaborador a una Capacitacion.
- `FechaInscripcion`, `Asistio` (bool), `Calificacion?` (decimal 5,2), `Observaciones?`
- FK: `ColaboradorId` → Colaborador, `CapacitacionId` → Capacitacion

### RecursoCapacitacion
Material adjunto a una capacitación.
- `Titulo`, `Url`, `Tipo` (enum `TipoRecurso`), `Descripcion?`, `Orden` (int)
- FK: `CapacitacionId` → Capacitacion

### Cuestionario
Evaluación asociada a una capacitación.
- `Titulo`, `Descripcion?`, `PuntajeAprobacion` (int, default 70)
- FK: `CapacitacionId` → Capacitacion (OnDelete: Cascade)
- Colecciones: `Preguntas`, `Respuestas`

### Pregunta
Pregunta de un cuestionario.
- `Texto`, `Orden` (int)
- FK: `CuestionarioId` → Cuestionario (OnDelete: Cascade)
- Colecciones: `Opciones` (OpcionRespuesta)

### OpcionRespuesta
Opción de respuesta para una pregunta.
- `Texto`, `EsCorrecta` (bool), `Orden` (int)
- FK: `PreguntaId` → Pregunta (OnDelete: Cascade)

### RespuestaCuestionario
Intento de un colaborador en un cuestionario.
- `FechaRespuesta`, `Puntaje` (decimal 5,2), `Aprobado` (bool), `TotalCorrectas` (int)
- FK: `InscripcionId` → Inscripcion (Restrict), `CuestionarioId` → Cuestionario (Restrict)
- Colecciones: `Respuestas` (RespuestaPregunta)

### RespuestaPregunta
Respuesta individual a una pregunta dentro de un intento.
- FK: `RespuestaCuestionarioId` → RespuestaCuestionario (Cascade)
- FK: `PreguntaId` → Pregunta (Restrict)
- FK: `OpcionElegidaId?` → OpcionRespuesta (Restrict)

---

## Certificados

### Certificado
Certificación obtenida por un colaborador.
- `Nombre`, `Institucion`, `FechaEmision`, `FechaVencimiento?`, `UrlDocumento?`
- FK: `ColaboradorId` → Colaborador

---

## Control Documental

### Documento
Documento oficial (política, procedimiento, etc.). Hereda `BaseEntity`.
- `Titulo`, `TipoDocumento` (enum → string), `Version` (string, ej: "1.0"), `Estado` (enum `EstadoDocumento` → string)
- `SharePointItemId`, `SharePointUrl`
- FK: `AreaId?` → Area
- Colecciones: `Versiones`, `Propuestas`, `FlujoAprobacion`

### VersionDocumento
Historial inmutable de versiones. **No hereda BaseEntity.**
- `NumeroVersion` (string), `SharePointItemId`, `FechaCreacion` (DateTime UTC)
- FK: `DocumentoId` → Documento

### PropuestaModificacion
Cambio propuesto por un colaborador sobre un documento publicado. **No hereda BaseEntity.**
- `Descripcion`, `SharePointItemIdPropuesta?`
- `EstadoPropuesta` (enum → string), `MotivoRechazo?`
- `FechaCreacion` (DateTime UTC), `FechaResolucion?`
- FK: `DocumentoId`, `ColaboradorId` (Restrict), `AreaId`, `AprobadorId?` (Restrict)

### FlujoAprobacionDoc
Registro inmutable de cada transición de estado. **No hereda BaseEntity.**
- `EstadoAnterior` (enum → string), `EstadoNuevo` (enum → string), `FechaTransicion` (DateTime UTC)
- FK: `DocumentoId`, `ColaboradorId` (Restrict)

---

## Cartas Laborales

### PlantillaDocumento
Plantilla para generar cartas laborales en PDF. Hereda `BaseEntity`.
- `Nombre`, `Descripcion?`, `TipoPlantilla` (enum: Html/Docx → string)
- `ContenidoHtml?` (HTML con marcadores `{{variable}}`), `ArchivoDocx?` (bytes)
- `FirmaImagenBase64?`, `NombreFirmante?`, `CargoFirmante?`
- `AplicaTodasAreas` (bool, default true), `VariablesEditables?` (JSON array de strings)
- Colecciones: `Areas` (PlantillaDocumentoArea), `Solicitudes` (SolicitudDocumento)

### PlantillaDocumentoArea
Relación muchos-a-muchos: PlantillaDocumento <-> Area. Clave compuesta `(PlantillaDocumentoId, AreaId)`.

### SolicitudDocumento
Solicitud de carta laboral — requiere aprobación del admin. Hereda `BaseEntity`.
- `FechaSolicitud` (DateTime UTC), `Estado` (enum `EstadoSolicitud` → string, default Pendiente)
- `PdfBytes?` (PDF generado al enviar), `ComentarioAdmin?`, `FechaResolucion?`
- FK: `PlantillaDocumentoId` (Restrict), `ColaboradorId` (Restrict)

---

## Auditoría

### AuditLog
Log de acciones del sistema. **No hereda BaseEntity — nunca se elimina.**
- FK: `ColaboradorId?` → Colaborador (OnDelete: SetNull)

---

## DbSets en AppDbContext

Colaboradores, Areas, Cargos, Certificados, Capacitaciones, Inscripciones,
RecursosCapacitacion, Documentos, VersionesDocumento, PropuestasModificacion,
FlujosAprobacionDoc, PlantillasDocumento, PlantillaDocumentoAreas,
SolicitudesDocumento, AuditLogs, Cuestionarios, Preguntas, OpcionesRespuesta,
RespuestasCuestionario, RespuestasPregunta

## Enums existentes (src/Domain/Enums/)

| Archivo | Enum | Valores |
|---|---|---|
| RolUsuario.cs | RolUsuario | Colaborador, Jefe, Admin |
| TipoRecurso.cs | TipoRecurso | Video, Documento, Presentacion, Enlace |
| DocumentoEnums.cs | TipoDocumento | Politica, Procedimiento, Contrato, Manual, Reglamento |
| DocumentoEnums.cs | EstadoDocumento | Borrador, Revision, Aprobado, Publicado |
| DocumentoEnums.cs | EstadoPropuesta | PendienteRevision, Aprobada, Rechazada |
| TipoPlantilla.cs | TipoPlantilla | Html, Docx |
| EstadoSolicitud.cs | EstadoSolicitud | Pendiente, Aprobada, Rechazada |

## Reglas de entidades inmutables

Las siguientes entidades NO heredan BaseEntity y no tienen soft delete:
- VersionDocumento, FlujoAprobacionDoc — registros históricos, nunca se modifican
- PropuestaModificacion — ciclo de vida propio (PendienteRevision → Aprobada/Rechazada)
- AuditLog — log de sistema, nunca se elimina
- RespuestaCuestionario, RespuestaPregunta — intentos de evaluación, inmutables