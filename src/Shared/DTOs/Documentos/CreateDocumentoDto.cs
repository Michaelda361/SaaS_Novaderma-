using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Documentos;

public class CreateDocumentoDto
{
    [Required(ErrorMessage = "El título es obligatorio")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo es obligatorio")]
    public string TipoDocumento { get; set; } = string.Empty;

    public int? AreaId { get; set; }

    // Opcional: URL externa (OneDrive/SharePoint) en lugar de subir archivo
    public string? UrlExterna { get; set; }
    public string? NombreArchivoExterno { get; set; }
}
