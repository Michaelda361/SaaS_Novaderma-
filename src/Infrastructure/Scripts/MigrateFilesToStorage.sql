-- ============================================================
-- Script de migración de datos viejos a storage
-- Ejecutar MANUALMENTE una vez que el storage esté configurado
-- y DESPUÉS de aplicar la migración MigrateFilesToStorage.
--
-- Este script NO elimina los binarios legacy — solo sirve como
-- referencia para identificar registros pendientes de migrar.
-- La eliminación de columnas legacy se hará en una migración
-- futura una vez confirmado que todos los FileKey están poblados.
-- ============================================================

-- 1. Ver plantillas con DOCX en SQL que aún no tienen DocxFileKey
SELECT Id, Nombre, TipoPlantilla,
       LEN(ArchivoDocx) AS TamanoDocxBytes,
       DocxFileKey
FROM PlantillasDocumento
WHERE ArchivoDocx IS NOT NULL
  AND DocxFileKey IS NULL
  AND Activo = 1;

-- 2. Ver solicitudes con PDF en SQL que aún no tienen PdfFileKey
SELECT Id, PlantillaDocumentoId, ColaboradorId, FechaSolicitud, Estado,
       LEN(PdfBytes) AS TamanoPdfBytes,
       PdfFileKey
FROM SolicitudesDocumento
WHERE PdfBytes IS NOT NULL
  AND PdfFileKey IS NULL
  AND Activo = 1;

-- 3. Ver certificados con PDF en SQL que aún no tienen PdfFileKey
SELECT Id, Nombre, ColaboradorId, FechaEmision,
       LEN(PdfBytes) AS TamanoPdfBytes,
       PdfFileKey
FROM Certificados
WHERE PdfBytes IS NOT NULL
  AND PdfFileKey IS NULL
  AND Activo = 1;

-- ============================================================
-- Una vez migrados todos los archivos al storage y verificado
-- que los FileKey están correctamente poblados, ejecutar:
-- ============================================================

-- VERIFICAR que no quedan registros sin FileKey antes de limpiar:
-- SELECT COUNT(*) FROM PlantillasDocumento WHERE ArchivoDocx IS NOT NULL AND DocxFileKey IS NULL;
-- SELECT COUNT(*) FROM SolicitudesDocumento WHERE PdfBytes IS NOT NULL AND PdfFileKey IS NULL;
-- SELECT COUNT(*) FROM Certificados WHERE PdfBytes IS NOT NULL AND PdfFileKey IS NULL;

-- Si los tres conteos son 0, se puede proceder con la migración
-- de eliminación de columnas legacy (crear nueva migración EF).
