using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Documentos;

public class RechazarPropuestaDto
{
    [Required(ErrorMessage = "El motivo de rechazo es obligatorio")]
    public string MotivoRechazo { get; set; } = string.Empty;
}
