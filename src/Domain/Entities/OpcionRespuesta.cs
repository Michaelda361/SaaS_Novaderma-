using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class OpcionRespuesta : BaseEntity
{
    public string Texto { get; set; } = string.Empty;
    public bool EsCorrecta { get; set; }
    public int Orden { get; set; }

    public int PreguntaId { get; set; }
    public Pregunta Pregunta { get; set; } = null!;
}
