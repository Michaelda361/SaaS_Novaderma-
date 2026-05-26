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

    public EstadoPropuesta EstadoPropuesta { get; set; } = EstadoPropuesta.PendienteRevision;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEdicion { get; set; }
    public DateTime? FechaResolucion { get; set; }
}
