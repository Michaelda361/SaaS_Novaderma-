namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class UpdateDocumentoResult
{
    public bool Exito { get; set; }
    public bool RequiereSolicitud { get; set; }
    public string? MensajeError { get; set; }
    public DocumentoControlDto? Documento { get; set; }
}
