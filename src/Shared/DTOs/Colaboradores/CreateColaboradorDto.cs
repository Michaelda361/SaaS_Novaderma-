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

    public string Telefono { get; set; } = string.Empty;

    [Required]
    public DateTime FechaIngreso { get; set; } = DateTime.Today;

    public string? Cedula { get; set; }
    public string? TipoContrato { get; set; }
    public decimal? SueldoBasico { get; set; }
    public string? Ciudad { get; set; }

    [Required(ErrorMessage = "El área es requerida")]
    public int AreaId { get; set; }

    [Required(ErrorMessage = "El cargo es requerido")]
    public int CargoId { get; set; }

    public int? SupervisorId { get; set; }
}
