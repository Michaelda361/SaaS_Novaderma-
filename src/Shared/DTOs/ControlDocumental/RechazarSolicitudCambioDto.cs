using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class RechazarSolicitudCambioDto
{
    [Required(ErrorMessage = "El motivo de rechazo es obligatorio")]
    public string MotivoRechazo { get; set; } = string.Empty;
}
