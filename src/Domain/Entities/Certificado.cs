using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Certificado : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? UrlDocumento { get; set; }

    /// <summary>PDF generado automáticamente desde la plantilla DOCX de la capacitación.</summary>
    public byte[]? PdfBytes { get; set; }

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    /// <summary>Si fue emitido automáticamente al aprobar una capacitación, referencia a ella.</summary>
    public int? CapacitacionId { get; set; }
    public Capacitacion? Capacitacion { get; set; }
}
