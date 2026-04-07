using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Cuestionario : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    /// <summary>Puntaje mínimo (0-100) para aprobar. Default 70.</summary>
    public int PuntajeAprobacion { get; set; } = 70;

    public int CapacitacionId { get; set; }
    public Capacitacion Capacitacion { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Pregunta> Preguntas { get; set; } = [];
    [JsonIgnore]
    public ICollection<RespuestaCuestionario> Respuestas { get; set; } = [];
}
