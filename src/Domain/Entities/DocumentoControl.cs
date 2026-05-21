using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class DocumentoControl : BaseEntity
{
    public int ListadoMaestroId { get; set; }
    public ListadoMaestro ListadoMaestro { get; set; } = null!;

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ProcesoResponsable { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public DateTime FechaDocumento { get; set; } = DateTime.UtcNow;

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

    public string? DatosPersonalizados { get; set; }

    public int? AreaId { get; set; }
    public Area? Area { get; set; }
}
