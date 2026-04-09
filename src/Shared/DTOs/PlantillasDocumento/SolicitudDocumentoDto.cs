namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

public class SolicitudDocumentoDto
{
    public int Id { get; set; }
    public int PlantillaDocumentoId { get; set; }
    public string PlantillaNombre { get; set; } = string.Empty;
    public int ColaboradorId { get; set; }
    public string ColaboradorNombre { get; set; } = string.Empty;
    public string ColaboradorEmail { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? ComentarioAdmin { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public bool TienePdf { get; set; }
}

public class ResolverSolicitudDto
{
    public string? Comentario { get; set; }
}

public class EnviarSolicitudDto
{
    public Dictionary<int, string> ParrafosEditados { get; set; } = [];
}
