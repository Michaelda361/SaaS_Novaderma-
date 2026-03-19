using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Cargos;

public class CreateCargoDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un área")]
    public int AreaId { get; set; }
}
