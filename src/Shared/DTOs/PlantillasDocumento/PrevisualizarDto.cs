namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

public class PrevisualizarDto
{
    public string Html { get; set; } = string.Empty;
    public string? NombreFirmante { get; set; }
    public string? CargoFirmante { get; set; }
    public string? FirmaImagenBase64 { get; set; }
    public string TipoPlantilla { get; set; } = "html";
    /// <summary>
    /// Valores resueltos del perfil del colaborador para las variables editables.
    /// Permite pre-rellenar los campos en el cliente.
    /// </summary>
    public Dictionary<string, string> ValoresPerfil { get; set; } = [];
}
