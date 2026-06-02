namespace TalentManagement.Shared.DTOs.Certificados;

public class CertificadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? UrlDocumento { get; set; }
    public int ColaboradorId { get; set; }
    public string ColaboradorNombre { get; set; } = string.Empty;

    /// <summary>Tipo MIME de la plantilla usada para emitir el certificado.</summary>
    public string? TipoArchivoCertificado { get; set; }

    /// <summary>Si fue emitido desde una capacitación, su Id.</summary>
    public int? CapacitacionId { get; set; }

    /// <summary>Nombre de la capacitación de origen, si aplica.</summary>
    public string? CapacitacionNombre { get; set; }
    public string? CertificateCode { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>True si tiene un PDF generado disponible para descargar.</summary>
    public bool TienePdf { get; set; }

    public bool EstaVencido => FechaVencimiento.HasValue && FechaVencimiento.Value < DateTime.Today;
    public bool VenceProximamente => FechaVencimiento.HasValue
        && !EstaVencido
        && FechaVencimiento.Value <= DateTime.Today.AddDays(30);
}
