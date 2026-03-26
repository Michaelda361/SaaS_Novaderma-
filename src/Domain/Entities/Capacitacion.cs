using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Capacitacion : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionHoras { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    // Asignación opcional: a un área o a un colaborador específico
    public int? AreaId { get; set; }
    public Area? Area { get; set; }

    public int? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }

    [JsonIgnore]
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
    [JsonIgnore]
    public ICollection<RecursoCapacitacion> Recursos { get; set; } = [];
}
