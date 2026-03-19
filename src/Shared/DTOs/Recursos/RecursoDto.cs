namespace TalentManagement.Shared.DTOs.Recursos;

public class RecursoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public int CapacitacionId { get; set; }
}
