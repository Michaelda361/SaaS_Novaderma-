namespace TalentManagement.Shared.DTOs.Cuestionarios;

/// <summary>
/// Respuesta del endpoint batch que devuelve los IDs de capacitaciones
/// aprobadas por un colaborador en una sola query.
/// </summary>
public class CapacitacionesAprobadasDto
{
    /// <summary>IDs de capacitaciones cuyo cuestionario fue aprobado por el colaborador.</summary>
    public List<int> CapacitacionIds { get; set; } = [];
}
