using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Pregunta : BaseEntity
{
    public string Enunciado { get; set; } = string.Empty;
    public int Orden { get; set; }

    public int CuestionarioId { get; set; }
    public Cuestionario Cuestionario { get; set; } = null!;

    [JsonIgnore]
    public ICollection<OpcionRespuesta> Opciones { get; set; } = [];
}
