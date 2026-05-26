namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class SolicitudCambioDocumentoControlDto
{
    public int Id { get; set; }
    public int DocumentoControlId { get; set; }
    public string DocumentoControlNombre { get; set; } = string.Empty;
    public string DocumentoControlCodigo { get; set; } = string.Empty;
    public int SolicitanteId { get; set; }
    public string SolicitanteNombre { get; set; } = string.Empty;
    public string EstadoPropuesta { get; set; } = string.Empty;
    public string ComentarioSolicitud { get; set; } = string.Empty;
    public string? ComentarioResolucion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEdicion { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string? DatosPropuestos { get; set; }
    public string? EditorNombre { get; set; }
    public int? AprobadorId { get; set; }
    public string? AprobadorNombre { get; set; }
}
