namespace TalentManagement.Shared.DTOs.Cuestionarios;

public class CuestionarioDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int PuntajeAprobacion { get; set; }
    public int CapacitacionId { get; set; }
    public List<PreguntaDto> Preguntas { get; set; } = [];
}

public class PreguntaDto
{
    public int Id { get; set; }
    public string Enunciado { get; set; } = string.Empty;
    public int Orden { get; set; }
    public List<OpcionDto> Opciones { get; set; } = [];
}

public class OpcionDto
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool EsCorrecta { get; set; }
    public int Orden { get; set; }
}
