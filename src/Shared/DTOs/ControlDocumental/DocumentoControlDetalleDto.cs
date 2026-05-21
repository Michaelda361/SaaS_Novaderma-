namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class DocumentoControlDetalleDto : DocumentoControlDto
{
    public string? OneDriveItemId { get; set; }
    public string? ArchivoNombre { get; set; }
    public string? Uso { get; set; }
    public string? TiempoRetencion { get; set; }
    public string? Proteccion { get; set; }
    public string? Recuperacion { get; set; }
    public string? DisposicionFinal { get; set; }
    public string? Observaciones { get; set; }
    public string? ComentarioCambio { get; set; }
    public string? ListadoMaestroDescripcion { get; set; }
    public Dictionary<string, string?>? CamposPersonalizados { get; set; } = new Dictionary<string, string?>();
}
