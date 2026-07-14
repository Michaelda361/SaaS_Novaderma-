namespace TalentManagement.Shared.DTOs.Areas;

public class AreaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? JefeId { get; set; }
    public string? JefeNombre { get; set; }
    public int TotalCargos { get; set; }
    public int TotalColaboradores { get; set; }
}
