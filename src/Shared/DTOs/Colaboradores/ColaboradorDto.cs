namespace TalentManagement.Shared.DTOs.Colaboradores;

public class ColaboradorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellido}";
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public string? Cedula { get; set; }
    public string? TipoContrato { get; set; }
    public decimal? SueldoBasico { get; set; }
    public string? Ciudad { get; set; }
    public string AreaNombre { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string CargoNombre { get; set; } = string.Empty;
    public int CargoId { get; set; }
    public string? SupervisorNombre { get; set; }
}
