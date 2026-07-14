namespace TalentManagement.Shared.DTOs.Colaboradores;

public class ColaboradorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellido}";
    public string Email { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public string? Cedula { get; set; }
    public DateTime? FechaExpedicion { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? LugarNacimiento { get; set; }
    public string? TipoContrato { get; set; }
    public DateTime? FechaIngresoContrato { get; set; }
    public decimal? SueldoBasico { get; set; }
    public decimal? SubTransporte { get; set; }
    public decimal? AuxMediosTransporte { get; set; }
    public decimal? AuxTransporte { get; set; }
    public decimal? ComisionVentas { get; set; }
    public decimal? ComisionCobros { get; set; }

    public decimal TotalSalario =>
        (SueldoBasico ?? 0m) +
        (SubTransporte ?? 0m) +
        (AuxMediosTransporte ?? 0m) +
        (ComisionVentas ?? 0m) +
        (ComisionCobros ?? 0m);

    public DateTime? FechaSalida { get; set; }
    public string AreaNombre { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string CargoNombre { get; set; } = string.Empty;
    public int CargoId { get; set; }
    public string Rol { get; set; } = "Colaborador";
    public Dictionary<string, string?> CamposAdicionales { get; set; } = new();

    /// <summary>GeneroColaborador como string (API / JSON).</summary>
    public string Genero { get; set; } = "NoInformado";
}

public class CambiarRolDto
{
    public string Rol { get; set; } = "Colaborador";
}
