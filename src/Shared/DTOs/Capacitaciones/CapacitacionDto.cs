namespace TalentManagement.Shared.DTOs.Capacitaciones;

public class CapacitacionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionHoras { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int TotalInscritos { get; set; }

    public int? AreaId { get; set; }
    public string? AreaNombre { get; set; }

    public int? ColaboradorId { get; set; }
    public string? ColaboradorNombre { get; set; }

    public bool EmiteCertificado { get; set; }
    public string? NombreCertificado { get; set; }
    public string? PlantillaNombreCertificado { get; set; }
    /// <summary>True si la capacitación ha sido finalizada.</summary>
    public bool Finalizada { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    /// <summary>True si la capacitación tiene un DOCX de plantilla de certificado.</summary>
    public bool TienePlantillaDocx { get; set; }

    /// <summary>MIME del archivo de plantilla del certificado. Null si no hay plantilla.</summary>
    public string? TipoArchivoCertificado { get; set; }
    /// <summary>True si está publicada y visible para colaboradores. False = Borrador.</summary>
    public bool Publicada { get; set; }

    /// <summary>Archivo PNG de la firma en formato Base64.</summary>
    public string? FirmaImagenBase64 { get; set; }

    /// <summary>Imagen de vista previa del certificado en formato Base64.</summary>
    public string? PreviewCertificadoBase64 { get; set; }


    /// <summary>Coordenada X de la firma en porcentaje (0-100).</summary>
    public double? FirmaX { get; set; }

    /// <summary>Coordenada Y de la firma en porcentaje (0-100).</summary>
    public double? FirmaY { get; set; }

    /// <summary>Ancho de la firma en porcentaje (0-100).</summary>
    public double? FirmaAncho { get; set; }

    /// <summary>Alto de la firma en porcentaje (0-100).</summary>
    public double? FirmaAlto { get; set; }

    public string TipoAsignacion => AreaId.HasValue ? "Área" : ColaboradorId.HasValue ? "Colaborador" : "General";
}

