using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Cuestionario : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    /// <summary>Puntaje mínimo (0-100) para aprobar. Default 70.</summary>
    public int PuntajeAprobacion { get; set; } = 70;

    /// <summary>Si true, la aprobación se decide por cantidad mínima de respuestas correctas.</summary>
    public bool AprobacionPorCorrectas { get; set; }

    /// <summary>Mínimo de respuestas correctas requeridas para aprobar cuando se usa el modo correcto.</summary>
    public int MinCorrectas { get; set; } = 1;

    /// <summary>Cantidad máxima de intentos permitidos para responder la evaluación. Default 1.</summary>
    public int IntentosPermitidos { get; set; } = 1;

    public int CapacitacionId { get; set; }
    public Capacitacion Capacitacion { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Pregunta> Preguntas { get; set; } = [];
    [JsonIgnore]
    public ICollection<RespuestaCuestionario> Respuestas { get; set; } = [];
}
