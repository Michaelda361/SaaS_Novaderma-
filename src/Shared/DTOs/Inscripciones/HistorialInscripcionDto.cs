using TalentManagement.Shared.DTOs.Cuestionarios;

namespace TalentManagement.Shared.DTOs.Inscripciones;

/// <summary>
/// Fila del historial completo: inscripción + resultado del cuestionario (si existe).
/// Devuelto por el endpoint batch GET /api/v1/inscripciones/historial-completo.
/// </summary>
public class HistorialInscripcionDto
{
    public InscripcionDto Inscripcion { get; set; } = null!;
    public ResultadoCuestionarioDto? Resultado { get; set; }
}
