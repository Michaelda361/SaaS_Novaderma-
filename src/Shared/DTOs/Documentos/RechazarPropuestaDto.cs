using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Documentos;

public class RechazarPropuestaDto
{
    [Required(ErrorMessage = "El motivo de rechazo es obligatorio")]
    [MaxLength(500, ErrorMessage = "El motivo de rechazo no puede superar 500 caracteres.")]
    public string MotivoRechazo { get; set; } = string.Empty;
}
