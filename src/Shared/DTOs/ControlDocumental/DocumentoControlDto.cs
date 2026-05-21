namespace TalentManagement.Shared.DTOs.ControlDocumental;

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
}
