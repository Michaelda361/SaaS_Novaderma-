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

    /// <summary>PDF generado al enviar — lo que el admin revisa.</summary>
    public byte[]? PdfBytes { get; set; }

    public string? ComentarioAdmin { get; set; }
    public DateTime? FechaResolucion { get; set; }
}
