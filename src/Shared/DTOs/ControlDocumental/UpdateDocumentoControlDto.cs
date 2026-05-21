using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class UpdateDocumentoControlDto
{
    [Required(ErrorMessage = "El listado maestro es obligatorio")]
    public int ListadoMaestroId { get; set; }

    [Required(ErrorMessage = "El código es obligatorio")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El proceso responsable es obligatorio")]
    public string ProcesoResponsable { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0";

    [Required(ErrorMessage = "La fecha del documento es obligatoria")]
    public DateTime FechaDocumento { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La URL de OneDrive es obligatoria")]
    [Url(ErrorMessage = "Ingresa una URL válida")]
    public string OneDriveUrl { get; set; } = string.Empty;

    public string? OneDriveItemId { get; set; }
    public string? ArchivoNombre { get; set; }
    public string? Uso { get; set; }
    public string? TiempoRetencion { get; set; }
    public string? Proteccion { get; set; }
    public string? Recuperacion { get; set; }
    public string? DisposicionFinal { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio")]
    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
    public string? ComentarioCambio { get; set; }
    public int? AreaId { get; set; }
    public Dictionary<string, string?>? CamposPersonalizados { get; set; } = new Dictionary<string, string?>();
}
