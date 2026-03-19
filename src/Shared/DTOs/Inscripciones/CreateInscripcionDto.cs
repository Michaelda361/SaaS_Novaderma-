using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Inscripciones;

public class CreateInscripcionDto
{
    [Required]
    public int ColaboradorId { get; set; }

    [Required]
    public int CapacitacionId { get; set; }

    public DateTime FechaInscripcion { get; set; } = DateTime.Today;
}
