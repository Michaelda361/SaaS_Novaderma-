namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class ListadoMaestroPermisoUpdateDto
{
    public int ColaboradorId { get; set; }
    public bool PuedeVer { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeAprobar { get; set; }
}
