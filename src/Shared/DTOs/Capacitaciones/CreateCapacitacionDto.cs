using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Capacitaciones;

public class CreateCapacitacionDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "La duración debe ser entre 1 y 1000 horas")]
    public int DuracionHoras { get; set; }

    [Required]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [Required]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(1);

    public int? AreaId { get; set; }
    public int? ColaboradorId { get; set; }

    /// <summary>Si true, se emite certificado automáticamente al aprobar el cuestionario.</summary>
    public bool EmiteCertificado { get; set; } = false;

    /// <summary>Nombre fijo del certificado. Si vacío, se usa el nombre de la capacitación.</summary>
    public string? NombreCertificado { get; set; }

    /// <summary>
    /// Plantilla con variables: {{nombre_completo}}, {{cargo}}, {{area}},
    /// {{capacitacion}}, {{fecha_emision}}, {{puntaje}}.
    /// Tiene prioridad sobre NombreCertificado si está definida.
    /// </summary>
    public string? PlantillaNombreCertificado { get; set; }
}
