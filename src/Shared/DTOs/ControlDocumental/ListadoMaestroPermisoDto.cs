namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class ListadoMaestroPermisoDto
{
    public int ColaboradorId { get; set; }
    public string ColaboradorNombre { get; set; } = string.Empty;
    public string ColaboradorEmail { get; set; } = string.Empty;
    public bool PuedeVer { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeAprobar { get; set; }
}
