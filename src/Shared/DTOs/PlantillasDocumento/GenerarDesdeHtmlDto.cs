namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

public class GenerarDesdeHtmlDto
{
    public string Html { get; set; } = string.Empty;
    public string? NombreFirmante { get; set; }
    public string? CargoFirmante { get; set; }
    public string? FirmaImagenBase64 { get; set; }
}
