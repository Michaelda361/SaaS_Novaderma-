using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class RutaAprendizaje : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public int? CargoId { get; set; }
    public Cargo? Cargo { get; set; }

    public ICollection<RutaCapacitacion> RutaCapacitaciones { get; set; } = [];
}
