using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string EntidadTipo { get; set; } = string.Empty;   // "Documento"
    public int EntidadId { get; set; }
    public string EntidadNombre { get; set; } = string.Empty; // título del doc
    public string Accion { get; set; } = string.Empty;        // "Creado", "EstadoCambiado", etc.
    public int? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }
    public string ColaboradorNombre { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }
    public string? CamposModificados { get; set; } // JSON: {"Estado":{"de":"Borrador","a":"Revision"}}
}
