-- Disable referential integrity
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all"

-- Clean transactional tables
DELETE FROM AuditLogs;
DELETE FROM RespuestasPregunta;
DELETE FROM RespuestasCuestionario;
DELETE FROM OpcionesRespuesta;
DELETE FROM Preguntas;
DELETE FROM Cuestionarios;

DELETE FROM Inscripciones;
DELETE FROM Certificados;
DELETE FROM RecursosCapacitacion;
DELETE FROM Capacitaciones;

DELETE FROM SolicitudesDocumento;
DELETE FROM FlujosAprobacionDoc;
DELETE FROM PropuestasModificacion;
DELETE FROM VersionesDocumento;
DELETE FROM Documentos;

DELETE FROM PlantillaDocumentoAreas;
DELETE FROM PlantillasDocumento;

-- Update Areas to remove JefeId before deleting Colaboradores
UPDATE Areas SET JefeId = NULL;
UPDATE Colaboradores SET SupervisorId = NULL;

-- Delete all users EXCEPT Michael (7) and Juan (5)
DELETE FROM Colaboradores WHERE Id NOT IN (5, 7);

-- Re-enable referential integrity
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"
