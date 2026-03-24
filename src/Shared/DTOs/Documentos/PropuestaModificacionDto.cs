namespace TalentManagement.Shared.DTOs.Documentos;

public class PropuestaModificacionDto
{
    public int Id { get; set; }
    public int DocumentoId { get; set; }
    public string DocumentoTitulo { get; set; } = string.Empty;
    public int ColaboradorId { get; set; }
    public string ColaboradorNombre { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string AreaNombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool TieneArchivo { get; set; }
    public string EstadoPropuesta { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaResolucion { get; set; }
}
