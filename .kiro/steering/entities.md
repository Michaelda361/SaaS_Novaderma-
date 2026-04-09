---
inclusion: always
---

# Entidades del Dominio — NovaHub

Catálogo completo de entidades existentes. Antes de proponer una nueva entidad, verificar que no exista ya aquí.

## Núcleo de Talento

### Colaborador
Empleado de la organización.
- `Nombre`, `Apellido`, `Email`, `Telefono`, `FechaIngreso`
- `Cedula`, `TipoContrato`, `SueldoBasico` (decimal), `Ciudad`
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
- `Titulo`, `Url`, `Tipo` (enum TipoRecurso: Enlace/Archivo/Video), `Descripcion?`, `Orden` (int)
- FK: `CapacitacionId` → Capacitacion

### Cuestionario
Evaluación asociada a una capacitación.
- `Titulo`, `Descripcion?`, `PuntajeAprobacion` (int, default 70)
- FK: `CapacitacionId` → Capacitacion
- Colecciones: `Preguntas`, `Respuestas`

### Pregunta / OpcionRespuesta / RespuestaCuestionario / RespuestaPregunta
Árbol de evaluación: Cuestionario → Preguntas → OpcionesRespuesta / RespuestaCuestionario → RespuestasPregunta

---

## Certificados

### Certificado
Certificación obtenida por un colaborador.
- `Nombre`, `Institucion`, `FechaEmision`, `FechaVencimiento?`, `UrlDocumento?`
- FK: `ColaboradorId` → Colaborador

---

## Control Documental

### Documento
Documento oficial (política, procedimiento, etc.).
- `Titulo`, `TipoDocumento` (enum → string), `Version` (string), `Estado` (enum EstadoDocumento → string)
- `SharePointItemId`, `SharePointUrl`
- FK: `AreaId?` → Area
- Colecciones: `Versiones`, `Propuestas`, `FlujoAprobacion`

### VersionDocumento / PropuestaModificacion / FlujoAprobacionDoc
Historial, propuestas de cambio y flujo de aprobación de un Documento.

---

## Cartas Laborales

### PlantillaDocumento
Plantilla para generar cartas laborales en PDF.
- `Nombre`, `Descripcion?`, `TipoPlantilla` (enum: Html/Docx)
- `ContenidoHtml?` (HTML con marcadores `{{variable}}`), `ArchivoDocx?` (bytes)
- `FirmaImagenBase64?`, `NombreFirmante?`, `CargoFirmante?`
- `AplicaTodasAreas` (bool), `VariablesEditables?` (JSON array de strings)

### PlantillaDocumentoArea
Relación muchos-a-muchos: PlantillaDocumento ↔ Area.

### SolicitudDocumento
Registro histórico de cada carta generada por un colaborador.
- `FechaSolicitud`, FK: `PlantillaDocumentoId`, `ColaboradorId`

---

## Auditoría

### AuditLog
Log de acciones del sistema. No hereda BaseEntity (sin soft delete).

---

## DbSets en AppDbContext

```
Colaboradores, Areas, Cargos, Certificados, Capacitaciones, Inscripciones,
RecursosCapacitacion, Documentos, VersionesDocumento, PropuestasModificacion,
FlujosAprobacionDoc, PlantillasDocumento, PlantillaDocumentoAreas,
SolicitudesDocumento, AuditLogs, Cuestionarios, Preguntas, OpcionesRespuesta,
RespuestasCuestionario, RespuestasPregunta
```

## Enums existentes (src/Domain/Enums/)

- `TipoRecurso`: Enlace, Archivo, Video
- `EstadoDocumento`: Borrador, EnRevision, Aprobado, Obsoleto
- `TipoPlantilla`: Html, Docx
- `TipoDocumento`: (ver archivo enum)
