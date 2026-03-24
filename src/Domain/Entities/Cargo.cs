using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Cargo : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
