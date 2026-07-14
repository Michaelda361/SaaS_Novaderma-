using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Cargo : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
