using TalentManagement.Domain.Common;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

/// <summary>Solicitud de carta laboral — requiere aprobación del admin antes de descargar.</summary>
public class SolicitudDocumento : BaseEntity
{
    public int PlantillaDocumentoId { get; set; }
    public PlantillaDocumento PlantillaDocumento { get; set; } = null!;

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    /// <summary>
    /// Clave en storage del PDF generado al enviar — lo que el admin revisa.
    /// Formato: "solicitudes-pdf/{guid}_{nombre.pdf}"
    /// </summary>
    public string? PdfFileKey { get; set; }

    // ── Compatibilidad temporal ──────────────────────────────────────────────
    /// <summary>
    /// OBSOLETO — binario heredado. Se mantiene solo para migración de datos viejos.
    /// Nuevas solicitudes usan PdfFileKey. Se eliminará en una migración futura.
    /// </summary>
    public byte[]? PdfBytes { get; set; }

    public string? ComentarioAdmin { get; set; }
    public DateTime? FechaResolucion { get; set; }

    /// <summary>False cuando hay una resolución nueva que el colaborador aún no ha visto.</summary>
    public bool NotificadoColaborador { get; set; } = true;

    /// <summary>JSON conteniendo las variables completadas y aplicadas al documento.</summary>
    public string? VariablesCompletadas { get; set; }

    /// <summary>Indica si el documento ya fue descargado por el colaborador (límite de 1 descarga).</summary>
    public bool Descargado { get; set; } = false;
}
