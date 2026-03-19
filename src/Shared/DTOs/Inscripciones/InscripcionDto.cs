namespace TalentManagement.Shared.DTOs.Inscripciones;

public class InscripcionDto
{
    public int Id { get; set; }
    public int ColaboradorId { get; set; }
    public string ColaboradorNombre { get; set; } = string.Empty;
    public string ColaboradorEmail { get; set; } = string.Empty;
    public string ColaboradorArea { get; set; } = string.Empty;
    public string ColaboradorCargo { get; set; } = string.Empty;
    public int CapacitacionId { get; set; }
    public string CapacitacionNombre { get; set; } = string.Empty;
    public DateTime FechaInscripcion { get; set; }
    public bool Asistio { get; set; }
    public decimal? Calificacion { get; set; }
    public string? Observaciones { get; set; }
}
