namespace TalentManagement.Domain.Entities;

public class VersionDocumento
{
    public int Id { get; set; }
    public int DocumentoId { get; set; }
    public Documento Documento { get; set; } = null!;
    public string NumeroVersion { get; set; } = string.Empty;
    public string SharePointItemId { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
