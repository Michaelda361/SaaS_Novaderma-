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
    /// <summary>True si la capacitación tiene un DOCX de plantilla de certificado.</summary>
    public bool TienePlantillaDocx { get; set; }
    /// <summary>True si está publicada y visible para colaboradores. False = Borrador.</summary>
    public bool Publicada { get; set; }

    public string TipoAsignacion => AreaId.HasValue ? "Área" : ColaboradorId.HasValue ? "Colaborador" : "General";
}
