namespace TalentManagement.Shared.DTOs.Capacitaciones;

public class CapacitacionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionHoras { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int TotalInscritos { get; set; }
}
