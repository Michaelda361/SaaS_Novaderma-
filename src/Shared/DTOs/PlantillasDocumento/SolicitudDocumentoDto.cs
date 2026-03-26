namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

public class SolicitudDocumentoDto
{
    public int Id { get; set; }
    public int PlantillaDocumentoId { get; set; }
    public string PlantillaNombre { get; set; } = string.Empty;
    public int ColaboradorId { get; set; }
    public string ColaboradorNombre { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
}
