namespace TalentManagement.Shared.DTOs.ControlDocumental;

public class ListadoMaestroDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public List<DocumentoControlCampoDto> Campos { get; set; } = [];
}
