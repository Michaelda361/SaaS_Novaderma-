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

    public int? AreaId { get; set; }
    public string? AreaNombre { get; set; }

    public int? ColaboradorId { get; set; }
    public string? ColaboradorNombre { get; set; }

    public string TipoAsignacion => AreaId.HasValue ? "Área" : ColaboradorId.HasValue ? "Colaborador" : "General";
}
