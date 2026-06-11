namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class DocumentoControlDetalleDto : DocumentoControlDto
{
    // Propiedades exclusivas del detalle — las demás (ArchivoNombre, Uso,
    // TiempoRetencion, Proteccion, Recuperacion, DisposicionFinal,
    // Observaciones, ComentarioCambio, CamposPersonalizados) ya están en la base.
    public string? OneDriveItemId { get; set; }
    public string? ListadoMaestroDescripcion { get; set; }
}
