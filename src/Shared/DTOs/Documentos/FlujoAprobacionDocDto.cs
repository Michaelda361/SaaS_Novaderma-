namespace TalentManagement.Shared.DTOs.Documentos;

public class FlujoAprobacionDocDto
{
    public int Id { get; set; }
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string ColaboradorNombre { get; set; } = string.Empty;
    public DateTime FechaTransicion { get; set; }
}
