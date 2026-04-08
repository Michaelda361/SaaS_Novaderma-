using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

/// <summary>Registro histórico de cada carta generada por un colaborador</summary>
public class SolicitudDocumento : BaseEntity
{
    public int PlantillaDocumentoId { get; set; }
    public PlantillaDocumento PlantillaDocumento { get; set; } = null!;

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
}
