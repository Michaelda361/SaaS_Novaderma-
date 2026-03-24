using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Documentos;

public class UpdateDocumentoDto
{
    [Required(ErrorMessage = "El título es obligatorio")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo es obligatorio")]
    public string TipoDocumento { get; set; } = string.Empty;

    public int? AreaId { get; set; }
}
