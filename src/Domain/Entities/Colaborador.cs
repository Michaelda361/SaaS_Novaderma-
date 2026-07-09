using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class Colaborador : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }

    public string? Cedula { get; set; }
    public DateTime? FechaExpedicion { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? LugarNacimiento { get; set; }

    public string? TipoContrato { get; set; }
    public DateTime? FechaIngresoContrato { get; set; }
    public decimal? SueldoBasico { get; set; }
    public decimal? SubTransporte { get; set; }
    public decimal? AuxMediosTransporte { get; set; }
    public decimal? AuxTransporte { get; set; }
    public decimal? ComisionVentas { get; set; }
    public decimal? ComisionCobros { get; set; }
    public DateTime? FechaSalida { get; set; }

    /// <summary>Género para tratamiento en documentos (cartas laborales). Cargado por RRHH.</summary>
    public GeneroColaborador Genero { get; set; } = GeneroColaborador.NoInformado;

    /// <summary>Rol del usuario en la aplicación.</summary>
    public RolUsuario Rol { get; set; } = RolUsuario.Colaborador;

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public int CargoId { get; set; }
    public Cargo Cargo { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Certificado> Certificados { get; set; } = [];
    [JsonIgnore]
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}
