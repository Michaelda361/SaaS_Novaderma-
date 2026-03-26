using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Colaborador : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }

    // Datos laborales para generación de cartas
    public string? Cedula { get; set; }
    public string? TipoContrato { get; set; }   // "término indefinido", "término fijo", etc.
    public decimal? SueldoBasico { get; set; }
    public string? Ciudad { get; set; }

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public int CargoId { get; set; }
    public Cargo Cargo { get; set; } = null!;

    public int? SupervisorId { get; set; }
    public Colaborador? Supervisor { get; set; }

    [JsonIgnore]
    public ICollection<Certificado> Certificados { get; set; } = [];
    [JsonIgnore]
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}
