using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Colaboradores;

public class CreateColaboradorDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    public string Apellido { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateTime FechaIngreso { get; set; } = DateTime.Today;

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

    /// <summary>NoInformado, Masculino, Femenino, OtroOPrefieroNoDecir</summary>
    public string Genero { get; set; } = "NoInformado";
    public Dictionary<string, string?>? CamposAdicionales { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El área es requerida")]
    public int AreaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El cargo es requerido")]
    public int CargoId { get; set; }
}
