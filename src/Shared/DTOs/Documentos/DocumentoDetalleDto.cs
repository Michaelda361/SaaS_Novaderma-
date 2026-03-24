namespace TalentManagement.Shared.DTOs.Documentos;

public class DocumentoDetalleDto : DocumentoDto
{
    public string SharePointUrl { get; set; } = string.Empty;
    public List<VersionDocumentoDto> Versiones { get; set; } = [];
    public List<FlujoAprobacionDocDto> Flujo { get; set; } = [];
    public List<PropuestaModificacionDto> Propuestas { get; set; } = [];
}
