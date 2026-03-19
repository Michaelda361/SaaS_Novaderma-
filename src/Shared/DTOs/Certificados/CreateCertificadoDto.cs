using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Certificados;

public class CreateCertificadoDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La institución es requerida")]
    public string Institucion { get; set; } = string.Empty;

    [Required]
    public DateTime FechaEmision { get; set; } = DateTime.Today;

    public DateTime? FechaVencimiento { get; set; }

    public string? UrlDocumento { get; set; }

    [Required]
    public int ColaboradorId { get; set; }
}
