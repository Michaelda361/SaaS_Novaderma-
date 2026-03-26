using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Area : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public int? JefeId { get; set; }
    public Colaborador? Jefe { get; set; }

    [JsonIgnore]
    public ICollection<Colaborador> Colaboradores { get; set; } = [];
    [JsonIgnore]
    public ICollection<Cargo> Cargos { get; set; } = [];
}
