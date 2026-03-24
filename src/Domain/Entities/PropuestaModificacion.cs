using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class PropuestaModificacion
{
    public int Id { get; set; }
    public int DocumentoId { get; set; }
    public Documento Documento { get; set; } = null!;

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public string Descripcion { get; set; } = string.Empty;
    public string? SharePointItemIdPropuesta { get; set; }

    public EstadoPropuesta EstadoPropuesta { get; set; } = EstadoPropuesta.PendienteRevision;

    public int? AprobadorId { get; set; }
    public Colaborador? Aprobador { get; set; }

    public string? MotivoRechazo { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaResolucion { get; set; }
}
