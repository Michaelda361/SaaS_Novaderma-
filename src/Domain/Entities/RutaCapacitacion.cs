namespace TalentManagement.Domain.Entities;

// Tabla intermedia entre RutaAprendizaje y Capacitacion
public class RutaCapacitacion
{
    public int Id { get; set; }
    public int Orden { get; set; }

    public int RutaAprendizajeId { get; set; }
    public RutaAprendizaje RutaAprendizaje { get; set; } = null!;

    public int CapacitacionId { get; set; }
    public Capacitacion Capacitacion { get; set; } = null!;
}
