namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

public class CampoFormularioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Texto corto"; // "Texto corto", "Texto largo", "Número", "Fecha", "Correo electrónico", "Selección única", "Selección múltiple"
    public bool Obligatorio { get; set; }
    public string Variable { get; set; } = string.Empty;
    public string? Opciones { get; set; }
    public string? DiligenciadoPor { get; set; } = "Colaborador"; // "Colaborador" or "Aprobador"
}
