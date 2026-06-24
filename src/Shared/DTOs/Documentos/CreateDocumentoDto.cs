using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Documentos;

public class CreateDocumentoDto
{
    [Required(ErrorMessage = "El título es obligatorio")]
    [MaxLength(200, ErrorMessage = "El título no puede superar 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo es obligatorio")]
    [MaxLength(100, ErrorMessage = "El tipo de documento no puede superar 100 caracteres.")]
    public string TipoDocumento { get; set; } = string.Empty;

    public int? AreaId { get; set; }

    // Opcional: URL externa (OneDrive/SharePoint) en lugar de subir archivo
    [MaxLength(1000, ErrorMessage = "La URL externa no puede superar 1000 caracteres.")]
    public string? UrlExterna { get; set; }

    [MaxLength(200, ErrorMessage = "El nombre del archivo externo no puede superar 200 caracteres.")]
    public string? NombreArchivoExterno { get; set; }
}
