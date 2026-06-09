using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class CreateDocumentoControlDto
{
    [Required(ErrorMessage = "El listado maestro es obligatorio")]
    public int ListadoMaestroId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string ProcesoResponsable { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0";

    public DateTime FechaDocumento { get; set; } = DateTime.Today;

    public string OneDriveUrl { get; set; } = string.Empty;

    public string? OneDriveItemId { get; set; }
    public string? ArchivoNombre { get; set; }
    public string? Uso { get; set; }
    public string? TiempoRetencion { get; set; }
    public string? Proteccion { get; set; }
    public string? Recuperacion { get; set; }
    public string? DisposicionFinal { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
    public string? ComentarioCambio { get; set; }
    public int? AreaId { get; set; }
    public Dictionary<string, string?>? CamposPersonalizados { get; set; } = new Dictionary<string, string?>();
}
