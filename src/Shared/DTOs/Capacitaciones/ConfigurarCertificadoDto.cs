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

    /// <summary>Archivo PNG de la firma en formato Base64.</summary>
    public string? FirmaImagenBase64 { get; set; }

    /// <summary>Coordenada X de la firma en porcentaje (0-100).</summary>
    public double? FirmaX { get; set; }

    /// <summary>Coordenada Y de la firma en porcentaje (0-100).</summary>
    public double? FirmaY { get; set; }

    /// <summary>Ancho de la firma en porcentaje (0-100).</summary>
    public double? FirmaAncho { get; set; }

    /// <summary>Alto de la firma en porcentaje (0-100).</summary>
    public double? FirmaAlto { get; set; }

    /// <summary>Indica si se desea eliminar la firma digital actual.</summary>
    public bool EliminarFirma { get; set; } = false;
}

