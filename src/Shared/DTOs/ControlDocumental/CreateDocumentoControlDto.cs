using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class CreateDocumentoControlDto
{
    [Required(ErrorMessage = "El listado maestro es obligatorio")]
    public int ListadoMaestroId { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El código no puede superar 50 caracteres.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El proceso responsable es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El proceso no puede superar 100 caracteres.")]
    public string ProcesoResponsable { get; set; } = string.Empty;

    [Required(ErrorMessage = "La versión es obligatoria.")]
    [MaxLength(10, ErrorMessage = "La versión no puede superar 10 caracteres.")]
    public string Version { get; set; } = "1.0";

    public DateTime FechaDocumento { get; set; } = DateTime.Today;

    [MaxLength(1000, ErrorMessage = "La URL de OneDrive no puede superar 1000 caracteres.")]
    public string OneDriveUrl { get; set; } = string.Empty;

    [MaxLength(250, ErrorMessage = "El Item ID de OneDrive no puede superar 250 caracteres.")]
    public string? OneDriveItemId { get; set; }

    [MaxLength(250, ErrorMessage = "El nombre del archivo no puede superar 250 caracteres.")]
    public string? ArchivoNombre { get; set; }

    [MaxLength(250, ErrorMessage = "El campo Uso no puede superar 250 caracteres.")]
    public string? Uso { get; set; }

    [MaxLength(250, ErrorMessage = "El tiempo de retención no puede superar 250 caracteres.")]
    public string? TiempoRetencion { get; set; }

    [MaxLength(250, ErrorMessage = "El campo Protección no puede superar 250 caracteres.")]
    public string? Proteccion { get; set; }

    [MaxLength(250, ErrorMessage = "El campo Recuperación no puede superar 250 caracteres.")]
    public string? Recuperacion { get; set; }

    [MaxLength(250, ErrorMessage = "El campo Disposición Final no puede superar 250 caracteres.")]
    public string? DisposicionFinal { get; set; }

    [MaxLength(50, ErrorMessage = "El estado no puede superar 50 caracteres.")]
    public string Estado { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Las observaciones no pueden superar 2000 caracteres.")]
    public string? Observaciones { get; set; }

    [MaxLength(1000, ErrorMessage = "El comentario de cambio no puede superar 1000 caracteres.")]
    public string? ComentarioCambio { get; set; }
    public int? AreaId { get; set; }
    public Dictionary<string, string?>? CamposPersonalizados { get; set; } = new Dictionary<string, string?>();
}
