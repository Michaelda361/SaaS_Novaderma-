namespace TalentManagement.Shared.DTOs.Documentos;

public class AuditLogDto
{
    public int Id { get; set; }
    public string EntidadTipo { get; set; } = string.Empty;
    public int EntidadId { get; set; }
    public string EntidadNombre { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string ColaboradorNombre { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string? Observaciones { get; set; }
    public string? CamposModificados { get; set; }
}
