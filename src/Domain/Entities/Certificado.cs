using TalentManagement.Domain.Common;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class Certificado : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? UrlDocumento { get; set; }

    /// <summary>
    /// Clave en storage del PDF del certificado.
    /// Formato: "certificados/{guid}_{nombre.pdf}"
    /// </summary>
    public string? PdfFileKey { get; set; }

    public string? CertificateCode { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }
    public CertificadoStatus Status { get; set; } = CertificadoStatus.Pending;

    // ── Compatibilidad temporal ──────────────────────────────────────────────
    /// <summary>
    /// OBSOLETO — binario heredado. Se mantiene solo para migración de datos viejos.
    /// Nuevos certificados usan PdfFileKey. Se eliminará en una migración futura.
    /// </summary>
    public byte[]? PdfBytes { get; set; }

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    /// <summary>Si fue emitido automáticamente al aprobar una capacitación, referencia a ella.</summary>
    public int? CapacitacionId { get; set; }
    public Capacitacion? Capacitacion { get; set; }

    public ICollection<CertificadoEvento> Eventos { get; set; } = new List<CertificadoEvento>();
}
