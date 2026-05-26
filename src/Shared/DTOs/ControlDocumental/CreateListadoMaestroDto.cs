using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class CreateListadoMaestroDto
{
    [Required(ErrorMessage = "El nombre del listado maestro es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public List<TemplateDocumentoDto> Documentos { get; set; } = [];
    public List<DocumentoControlCampoDto> Campos { get; set; } = [];
}

public class TemplateDocumentoDto
{
    [Required(ErrorMessage = "El código del documento es obligatorio")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del documento es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El proceso responsable es obligatorio")]
    public string ProcesoResponsable { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0";
    public string Estado { get; set; } = "Borrador";
    public DateTime? FechaDocumento { get; set; }
    public string? OneDriveUrl { get; set; }
    public string? OneDriveItemId { get; set; }
    public string? ArchivoNombre { get; set; }
    public string? Uso { get; set; }
    public string? TiempoRetencion { get; set; }
    public string? Proteccion { get; set; }
    public string? Recuperacion { get; set; }
    public string? DisposicionFinal { get; set; }
    public string? Observaciones { get; set; }
    public string? ComentarioCambio { get; set; }
    public string? Area { get; set; }
    public Dictionary<string, string?> CamposPersonalizados { get; set; } = new();
}
