using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Cuestionarios;
using TalentManagement.Shared.DTOs.Inscripciones;

namespace TalentManagement.Application.Services;

public class InscripcionService(IInscripcionRepository repository)
{
    public async Task<List<InscripcionDto>> GetByCapacitacionAsync(int capacitacionId)
    {
        var items = await repository.GetByCapacitacionAsync(capacitacionId);
        return items.Select(MapToDto).ToList();
    }

    // Para el historial del admin: incluye inscripciones de capacitaciones eliminadas
    public async Task<List<InscripcionDto>> GetByCapacitacionHistorialAsync(int capacitacionId)
    {
        var items = await repository.GetByCapacitacionIgnorandoFiltrosAsync(capacitacionId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<List<InscripcionDto>> GetByColaboradorAsync(int colaboradorId)
    {
        var items = await repository.GetByColaboradorAsync(colaboradorId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<InscripcionDto?> GetByIdAsync(int id)
    {
        var item = await repository.GetByIdAsync(id);
        return item is null ? null : MapToDto(item);
    }

    public async Task<(InscripcionDto? result, string? error)> CreateAsync(CreateInscripcionDto dto)
    {
        // AnyAsync puntual — sin cargar toda la lista
        if (await repository.ExisteInscripcionAsync(dto.CapacitacionId, dto.ColaboradorId))
            return (null, "El colaborador ya está inscrito en esta capacitación.");

        var inscripcion = new Inscripcion
        {
            ColaboradorId = dto.ColaboradorId,
            CapacitacionId = dto.CapacitacionId,
            FechaInscripcion = dto.FechaInscripcion
        };

        var created = await repository.CreateAsync(inscripcion);
        return (MapToDto(created), null);
    }

    public async Task<InscripcionDto?> UpdateAsync(int id, UpdateInscripcionDto dto)
    {
        var inscripcion = await repository.GetByIdAsync(id);
        if (inscripcion is null) return null;

        inscripcion.Asistio = dto.Asistio;
        inscripcion.Calificacion = dto.Calificacion;
        inscripcion.Observaciones = dto.Observaciones;

        var updated = await repository.UpdateAsync(inscripcion);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id) => await repository.DeleteAsync(id);

    public Task<bool> ExisteInscripcionAsync(int capacitacionId, int colaboradorId) =>
        repository.ExisteInscripcionAsync(capacitacionId, colaboradorId);

    /// <summary>
    /// Devuelve el historial completo (inscripciones + resultados) en 3 queries planas.
    /// Reemplaza el N+1 masivo de CargarHistorial en Capacitaciones.razor.
    /// </summary>
    public async Task<List<HistorialInscripcionDto>> GetHistorialCompletoAsync()
    {
        var filas = await repository.GetHistorialCompletoAsync();

        return filas.Select(f =>
        {
            var (insc, respuesta, cuestionario) = f;
            ResultadoCuestionarioDto? resultado = null;
            if (respuesta is not null)
            {
                resultado = new ResultadoCuestionarioDto
                {
                    Puntaje           = respuesta.Puntaje,
                    Aprobado          = respuesta.Aprobado,
                    PuntajeAprobacion = cuestionario?.PuntajeAprobacion ?? 70,
                    TotalPreguntas    = cuestionario?.Preguntas.Count ?? 0,
                    Correctas         = respuesta.TotalCorrectas
                };
            }
            return new HistorialInscripcionDto
            {
                Inscripcion = MapToDto(insc),
                Resultado   = resultado
            };
        }).ToList();
    }

    public async Task<List<HistorialInscripcionDto>> GetHistorialCompletoAsync(int colaboradorId)
    {
        var filas = await repository.GetHistorialCompletoAsync(colaboradorId);

        return filas.Select(f =>
        {
            var (insc, respuesta, cuestionario) = f;
            ResultadoCuestionarioDto? resultado = null;
            if (respuesta is not null)
            {
                resultado = new ResultadoCuestionarioDto
                {
                    Puntaje           = respuesta.Puntaje,
                    Aprobado          = respuesta.Aprobado,
                    PuntajeAprobacion = cuestionario?.PuntajeAprobacion ?? 70,
                    TotalPreguntas    = cuestionario?.Preguntas.Count ?? 0,
                    Correctas         = respuesta.TotalCorrectas
                };
            }
            return new HistorialInscripcionDto
            {
                Inscripcion = MapToDto(insc),
                Resultado   = resultado
            };
        }).ToList();
    }

    private static InscripcionDto MapToDto(Inscripcion i) => new()
    {
        Id = i.Id,
        ColaboradorId = i.ColaboradorId,
        ColaboradorNombre = $"{i.Colaborador.Nombre} {i.Colaborador.Apellido}",
        ColaboradorEmail = i.Colaborador.Email,
        ColaboradorArea = i.Colaborador.Area?.Nombre ?? "-",
        ColaboradorCargo = i.Colaborador.Cargo?.Nombre ?? "-",
        CapacitacionId = i.CapacitacionId,
        CapacitacionNombre = i.Capacitacion.Nombre,
        FechaInscripcion = i.FechaInscripcion,
        FechaFinalizacion = i.Capacitacion?.FechaFinalizacion,
        Asistio = i.Asistio,
        Calificacion = i.Calificacion,
        Observaciones = i.Observaciones
    };
}
