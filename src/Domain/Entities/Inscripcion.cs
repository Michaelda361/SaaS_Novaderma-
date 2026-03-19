namespace TalentManagement.Domain.Entities;

public class Inscripcion
{
    public int Id { get; set; }
    public DateTime FechaInscripcion { get; set; }
    public bool Asistio { get; set; }
    public decimal? Calificacion { get; set; }
    public string? Observaciones { get; set; }

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    public int CapacitacionId { get; set; }
    public Capacitacion Capacitacion { get; set; } = null!;
}
