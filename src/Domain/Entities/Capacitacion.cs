using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Capacitacion : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionHoras { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}
