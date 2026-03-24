using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Documentos;

public class CreatePropuestaDto
{
    [Required(ErrorMessage = "La descripción del cambio es obligatoria")]
    [MinLength(10, ErrorMessage = "La descripción debe tener al menos 10 caracteres")]
    public string Descripcion { get; set; } = string.Empty;
}
