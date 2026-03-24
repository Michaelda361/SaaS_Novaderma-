namespace TalentManagement.Shared.DTOs.Documentos;

public class VersionDocumentoDto
{
    public int Id { get; set; }
    public string NumeroVersion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
