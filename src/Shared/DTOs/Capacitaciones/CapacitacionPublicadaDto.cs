namespace TalentManagement.Shared.DTOs.Capacitaciones;

/// <summary>Payload enviado via SignalR cuando una capacitación es publicada.</summary>
public class CapacitacionPublicadaDto
{
    public int CapacitacionId { get; set; }
    public string CapacitacionNombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
