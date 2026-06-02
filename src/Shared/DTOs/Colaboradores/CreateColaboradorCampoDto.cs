namespace TalentManagement.Shared.DTOs.Colaboradores;

public class CreateColaboradorCampoDto
{
    /// <summary>
    /// Optional explicit technical key for the field. If omitted it will be generated from the <see cref="Nombre"/>.
    /// Use this when creating definitions for standard fields to preserve their internal key.
    /// </summary>
    public string? CampoClave { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Texto";
    public bool Requerido { get; set; }
    public string? Opciones { get; set; }
    public int Orden { get; set; }
}
