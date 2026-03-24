namespace TalentManagement.Shared.DTOs.Documentos;

public class DocumentoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int? AreaId { get; set; }
    public string? AreaNombre { get; set; }
}
