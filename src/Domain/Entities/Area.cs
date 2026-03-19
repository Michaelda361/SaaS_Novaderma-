using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Area : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public int? JefeId { get; set; }
    public Colaborador? Jefe { get; set; }

    public ICollection<Colaborador> Colaboradores { get; set; } = [];
    public ICollection<Cargo> Cargos { get; set; } = [];
}
