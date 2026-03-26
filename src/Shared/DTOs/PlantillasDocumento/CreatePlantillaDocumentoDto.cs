using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

public class CreatePlantillaDocumentoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    /// <summary>"html" o "docx"</summary>
    public string TipoPlantilla { get; set; } = "html";

    public string? ContenidoHtml { get; set; }

    /// <summary>Bytes del .docx en Base64 (se envía desde el cliente)</summary>
    public string? ArchivoDocxBase64 { get; set; }

    public string? FirmaImagenBase64 { get; set; }
    public string? NombreFirmante { get; set; }
    public string? CargoFirmante { get; set; }

    public bool AplicaTodasAreas { get; set; } = true;
    public List<int> AreaIds { get; set; } = [];
}
