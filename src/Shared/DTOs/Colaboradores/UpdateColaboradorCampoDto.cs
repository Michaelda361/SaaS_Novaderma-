namespace TalentManagement.Shared.DTOs.Colaboradores;

public class UpdateColaboradorCampoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Texto";
    public bool Requerido { get; set; }
    public string? Opciones { get; set; }
    public int Orden { get; set; }
}
