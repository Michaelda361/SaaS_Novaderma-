using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class CreateListadoMaestroDto
{
    [Required(ErrorMessage = "El nombre del listado maestro es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
