namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class DocumentoControlCampoDto
{
    public string CampoClave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Texto";
    public bool Requerido { get; set; }
    public bool EsPredeterminado { get; set; }
    public string? Opciones { get; set; }
    public int Orden { get; set; }
}
