using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

/// <summary>Opción elegida por el colaborador en cada pregunta.</summary>
public class RespuestaPregunta : BaseEntity
{
    public int RespuestaCuestionarioId { get; set; }
    public RespuestaCuestionario RespuestaCuestionario { get; set; } = null!;

    public int PreguntaId { get; set; }
    public Pregunta Pregunta { get; set; } = null!;

    public int OpcionElegidaId { get; set; }
    public OpcionRespuesta OpcionElegida { get; set; } = null!;
}
