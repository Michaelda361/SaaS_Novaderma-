namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

public class PrevisualizarDto
{
    public string Html { get; set; } = string.Empty;
    public string? NombreFirmante { get; set; }
    public string? CargoFirmante { get; set; }
    public string? FirmaImagenBase64 { get; set; }
    public string TipoPlantilla { get; set; } = "html";
}
