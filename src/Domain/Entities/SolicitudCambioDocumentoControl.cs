using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class SolicitudCambioDocumentoControl
{
    public int Id { get; set; }

    public int DocumentoControlId { get; set; }
    public DocumentoControl DocumentoControl { get; set; } = null!;

    public int SolicitanteId { get; set; }
    public Colaborador Solicitante { get; set; } = null!;

    public int? EditorId { get; set; }
    public Colaborador? Editor { get; set; }

    public int? AprobadorId { get; set; }
    public Colaborador? Aprobador { get; set; }

    public string ComentarioSolicitud { get; set; } = string.Empty;
    public string? ComentarioResolucion { get; set; }

    public string DatosPropuestos { get; set; } = string.Empty;
    public string? DatosOriginales { get; set; }

    public EstadoPropuesta EstadoPropuesta { get; set; } = EstadoPropuesta.PendienteRevision;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEdicion { get; set; }
    public DateTime? FechaResolucion { get; set; }

    // Revisor y observaciones de revisión
    public int? RevisorId { get; set; }
    public Colaborador? Revisor { get; set; }
    public DateTime? FechaRevision { get; set; }
    public string? ObservacionesRevision { get; set; }

    // Detalles obligatorios de la solicitud
    public string MotivoCambio { get; set; } = string.Empty;
    public string DescripcionDetallada { get; set; } = string.Empty;

    // Enlace al borrador creado para edición
    public int? BorradorDocumentoId { get; set; }
    public DocumentoControl? BorradorDocumento { get; set; }
}
