using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Areas;

public class CreateAreaDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int? JefeId { get; set; }
}
