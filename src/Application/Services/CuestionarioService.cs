using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Cuestionarios;

namespace TalentManagement.Application.Services;

public class CuestionarioService(ICuestionarioRepository repository)
{
    public async Task<CuestionarioDto?> GetByCapacitacionAsync(int capacitacionId)
    {
        var c = await repository.GetByCapacitacionAsync(capacitacionId);
        return c is null ? null : MapToDto(c);
    }

    public async Task<CuestionarioDto> CreateAsync(CreateCuestionarioDto dto)
    {
        var cuestionario = new Cuestionario
        {
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            PuntajeAprobacion = dto.PuntajeAprobacion,
            CapacitacionId = dto.CapacitacionId,
            Preguntas = dto.Preguntas.Select((p, pi) => new Pregunta
            {
                Enunciado = p.Enunciado,
                Orden = p.Orden > 0 ? p.Orden : pi + 1,
                Opciones = p.Opciones.Select((o, oi) => new OpcionRespuesta
                {
                    Texto = o.Texto,
                    EsCorrecta = o.EsCorrecta,
                    Orden = o.Orden > 0 ? o.Orden : oi + 1
                }).ToList()
            }).ToList()
        };

        var created = await repository.CreateAsync(cuestionario);
        return MapToDto(created);
    }

    public async Task<CuestionarioDto?> UpdateAsync(int id, CreateCuestionarioDto dto)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null) return null;

        existing.Titulo = dto.Titulo;
        existing.Descripcion = dto.Descripcion;
        existing.PuntajeAprobacion = dto.PuntajeAprobacion;

        // Reemplazar preguntas completas
        existing.Preguntas = dto.Preguntas.Select((p, pi) => new Pregunta
        {
            Enunciado = p.Enunciado,
            Orden = p.Orden > 0 ? p.Orden : pi + 1,
            CuestionarioId = id,
            Opciones = p.Opciones.Select((o, oi) => new OpcionRespuesta
            {
                Texto = o.Texto,
                EsCorrecta = o.EsCorrecta,
                Orden = o.Orden > 0 ? o.Orden : oi + 1
            }).ToList()
        }).ToList();

        var updated = await repository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var c = await repository.GetByIdAsync(id);
        if (c is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<ResultadoCuestionarioDto> ResponderAsync(ResponderCuestionarioDto dto)
    {
        var cuestionario = await repository.GetByIdAsync(dto.CuestionarioId)
            ?? throw new InvalidOperationException("Cuestionario no encontrado.");

        // Solo se permite un intento — si ya respondió, devolver el resultado existente
        var existente = await repository.GetRespuestaAsync(dto.CuestionarioId, dto.InscripcionId);
        if (existente is not null)
            return new ResultadoCuestionarioDto
            {
                Puntaje = existente.Puntaje,
                Aprobado = existente.Aprobado,
                PuntajeAprobacion = cuestionario.PuntajeAprobacion,
                TotalPreguntas = cuestionario.Preguntas.Count,
                Correctas = existente.TotalCorrectas
            };

        int correctas = 0;
        var respuestasEntidad = new List<RespuestaPregunta>();

        foreach (var r in dto.Respuestas)
        {
            var pregunta = cuestionario.Preguntas.FirstOrDefault(p => p.Id == r.PreguntaId);
            if (pregunta is null) continue;

            var opcion = pregunta.Opciones.FirstOrDefault(o => o.Id == r.OpcionElegidaId);
            if (opcion?.EsCorrecta == true) correctas++;

            respuestasEntidad.Add(new RespuestaPregunta
            {
                PreguntaId = r.PreguntaId,
                OpcionElegidaId = r.OpcionElegidaId
            });
        }

        int total = cuestionario.Preguntas.Count;
        decimal puntaje = total > 0 ? Math.Round((decimal)correctas / total * 100, 2) : 0;
        bool aprobado = puntaje >= cuestionario.PuntajeAprobacion;

        var respuesta = new RespuestaCuestionario
        {
            CuestionarioId = dto.CuestionarioId,
            InscripcionId = dto.InscripcionId,
            FechaRespuesta = DateTime.UtcNow,
            Puntaje = puntaje,
            Aprobado = aprobado,
            TotalCorrectas = correctas,
            Respuestas = respuestasEntidad
        };

        await repository.SaveRespuestaAsync(respuesta);

        return new ResultadoCuestionarioDto
        {
            Puntaje = puntaje,
            Aprobado = aprobado,
            PuntajeAprobacion = cuestionario.PuntajeAprobacion,
            TotalPreguntas = total,
            Correctas = correctas
        };
    }

    public async Task<ResultadoCuestionarioDto?> GetResultadoAsync(int cuestionarioId, int inscripcionId)
    {
        var r = await repository.GetRespuestaAsync(cuestionarioId, inscripcionId);
        if (r is null) return null;
        var c = await repository.GetByIdAsync(cuestionarioId);
        return new ResultadoCuestionarioDto
        {
            Puntaje = r.Puntaje,
            Aprobado = r.Aprobado,
            PuntajeAprobacion = c?.PuntajeAprobacion ?? 70,
            TotalPreguntas = c?.Preguntas.Count ?? 0,
            Correctas = r.TotalCorrectas
        };
    }

    private static CuestionarioDto MapToDto(Cuestionario c) => new()
    {
        Id = c.Id,
        Titulo = c.Titulo,
        Descripcion = c.Descripcion,
        PuntajeAprobacion = c.PuntajeAprobacion,
        CapacitacionId = c.CapacitacionId,
        Preguntas = c.Preguntas.OrderBy(p => p.Orden).Select(p => new PreguntaDto
        {
            Id = p.Id,
            Enunciado = p.Enunciado,
            Orden = p.Orden,
            Opciones = p.Opciones.OrderBy(o => o.Orden).Select(o => new OpcionDto
            {
                Id = o.Id,
                Texto = o.Texto,
                EsCorrecta = o.EsCorrecta,
                Orden = o.Orden
            }).ToList()
        }).ToList()
    };
}
