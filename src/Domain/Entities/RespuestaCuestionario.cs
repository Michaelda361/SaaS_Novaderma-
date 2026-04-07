using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

/// <summary>Intento de un colaborador al responder un cuestionario.</summary>
public class RespuestaCuestionario : BaseEntity
{
    public int CuestionarioId { get; set; }
    public Cuestionario Cuestionario { get; set; } = null!;

    public int InscripcionId { get; set; }
    public Inscripcion Inscripcion { get; set; } = null!;

    public DateTime FechaRespuesta { get; set; } = DateTime.UtcNow;

    /// <summary>Puntaje obtenido 0-100.</summary>
    public decimal Puntaje { get; set; }

    public bool Aprobado { get; set; }

    [JsonIgnore]
    public ICollection<RespuestaPregunta> Respuestas { get; set; } = [];
}
