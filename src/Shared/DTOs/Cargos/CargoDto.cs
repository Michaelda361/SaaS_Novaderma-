namespace TalentManagement.Shared.DTOs.Cargos;

public class CargoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int AreaId { get; set; }
    public string AreaNombre { get; set; } = string.Empty;
    public int TotalColaboradores { get; set; }
}
