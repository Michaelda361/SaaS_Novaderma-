using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Colaboradores;

public class UpdateColaboradorDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Apellido { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string? Cedula { get; set; }
    public string? TipoContrato { get; set; }
    public decimal? SueldoBasico { get; set; }
    public string? Ciudad { get; set; }

    /// <summary>NoInformado, Masculino, Femenino, OtroOPrefieroNoDecir</summary>
    public string Genero { get; set; } = "NoInformado";

    [Required]
    public int AreaId { get; set; }

    [Required]
    public int CargoId { get; set; }

    public int? SupervisorId { get; set; }
}
