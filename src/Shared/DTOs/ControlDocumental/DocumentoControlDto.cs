namespace TalentManagement.Shared.DTOs.ControlDocumental;

using System;
using System.Collections.Generic;

public class DocumentoControlDto
{
    public int Id { get; set; }
    public int ListadoMaestroId { get; set; }
    public string? ListadoMaestroNombre { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ProcesoResponsable { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime FechaDocumento { get; set; }
    public string OneDriveUrl { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int? AreaId { get; set; }
    public string? AreaNombre { get; set; }

    public string? Uso { get; set; }
    public string? TiempoRetencion { get; set; }
    public string? Proteccion { get; set; }
    public string? Recuperacion { get; set; }
    public string? DisposicionFinal { get; set; }
    public string? Observaciones { get; set; }
    public string? ComentarioCambio { get; set; }
    public string? ArchivoNombre { get; set; }
    public Dictionary<string, string?>? CamposPersonalizados { get; set; } = new Dictionary<string, string?>();

    // Versioning and traceability fields
    public int? DocumentoOriginalId { get; set; }
    public int? SolicitanteId { get; set; }
    public string? SolicitanteNombre { get; set; }
    public int? EditorId { get; set; }
    public string? EditorNombre { get; set; }
    public int? AprobadorId { get; set; }
    public string? AprobadorNombre { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public string? MotivoCambio { get; set; }
    public string? DescripcionDetallada { get; set; }
}
