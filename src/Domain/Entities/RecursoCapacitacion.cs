namespace TalentManagement.Domain.Entities;

public enum TipoRecurso
{
    Video,
    Documento,
    Presentacion,
    Enlace
}

public class RecursoCapacitacion
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public TipoRecurso Tipo { get; set; } = TipoRecurso.Enlace;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }

    public int CapacitacionId { get; set; }
    public Capacitacion Capacitacion { get; set; } = null!;
}
