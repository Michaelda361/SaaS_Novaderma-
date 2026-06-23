namespace TalentManagement.Shared.DTOs.Capacitaciones;

/// <summary>
/// Payload para activar/desactivar y configurar el certificado de una capacitación
/// sin necesidad de editar todos los campos de la capacitación.
/// </summary>
public class ConfigurarCertificadoDto
{
    public bool EmiteCertificado { get; set; }

    /// <summary>
    /// Plantilla del nombre con variables:
    /// {{nombre_completo}}, {{cargo}}, {{area}}, {{capacitacion}}, {{fecha_emision}}, {{puntaje}}
    /// Si vacío, se usa el nombre de la capacitación.
    /// </summary>
    public string? PlantillaNombreCertificado { get; set; }

    /// <summary>
    /// Archivo DOCX, PPTX o PDF en Base64. Si se envía, se guarda como plantilla del certificado.
    /// Soporta las mismas variables que PlantillaNombreCertificado.
    /// </summary>
    public string? ArchivoDocxBase64 { get; set; }

    /// <summary>Tipo MIME del archivo, e.g. DOCX, PPTX o PDF.</summary>
    public string? TipoArchivoCertificado { get; set; }

    /// <summary>True si se quiere eliminar el DOCX/PPTX existente sin subir uno nuevo.</summary>
    public bool EliminarDocx { get; set; } = false;

    /// <summary>ID de otra capacitación desde la cual copiar la plantilla y nombre del certificado.</summary>
    public int? CopiarPlantillaDesdeId { get; set; }
}
