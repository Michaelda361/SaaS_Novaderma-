using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class Colaborador : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }

    public string? Cedula { get; set; }
    public string? TipoContrato { get; set; }
    public decimal? SueldoBasico { get; set; }
    public string? Ciudad { get; set; }

    /// <summary>Rol del usuario en la aplicación.</summary>
    public RolUsuario Rol { get; set; } = RolUsuario.Colaborador;

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
