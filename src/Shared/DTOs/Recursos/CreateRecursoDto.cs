using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Recursos;

public class CreateRecursoDto
{
    [Required(ErrorMessage = "El título es requerido")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La URL es requerida")]
    [Url(ErrorMessage = "Ingresa una URL válida")]
    public string Url { get; set; } = string.Empty;

    public string Tipo { get; set; } = "Enlace";
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public int CapacitacionId { get; set; }
}
