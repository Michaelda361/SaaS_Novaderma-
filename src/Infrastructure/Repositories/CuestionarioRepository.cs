using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class CuestionarioRepository(AppDbContext context) : ICuestionarioRepository
{
    private IQueryable<Cuestionario> WithIncludes() =>
        context.Cuestionarios
            .Include(c => c.Preguntas.Where(p => p.Activo))
                .ThenInclude(p => p.Opciones.Where(o => o.Activo));

    public async Task<Cuestionario?> GetByCapacitacionAsync(int capacitacionId) =>
        await WithIncludes().FirstOrDefaultAsync(c => c.CapacitacionId == capacitacionId);

    public async Task<Cuestionario?> GetByIdAsync(int id) =>
        await WithIncludes().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Cuestionario> CreateAsync(Cuestionario cuestionario)
    {
        context.Cuestionarios.Add(cuestionario);
        await context.SaveChangesAsync();
        return (await GetByIdAsync(cuestionario.Id))!;
    }

    public async Task<Cuestionario> UpdateAsync(Cuestionario cuestionario)
    {
        // Soft-delete preguntas y opciones anteriores, luego agregar las nuevas
        var existing = await context.Cuestionarios
            .Include(c => c.Preguntas).ThenInclude(p => p.Opciones)
            .FirstOrDefaultAsync(c => c.Id == cuestionario.Id);

        if (existing is not null)
        {
            foreach (var p in existing.Preguntas)
            {
                foreach (var o in p.Opciones) o.Activo = false;
                p.Activo = false;
            }
            existing.Titulo = cuestionario.Titulo;
            existing.Descripcion = cuestionario.Descripcion;
            existing.PuntajeAprobacion = cuestionario.PuntajeAprobacion;
            existing.AprobacionPorCorrectas = cuestionario.AprobacionPorCorrectas;
            existing.MinCorrectas = cuestionario.MinCorrectas;
            existing.IntentosPermitidos = cuestionario.IntentosPermitidos;

            foreach (var p in cuestionario.Preguntas)
            {
                p.CuestionarioId = cuestionario.Id;
                context.Preguntas.Add(p);
            }
        }

        await context.SaveChangesAsync();
        return (await GetByIdAsync(cuestionario.Id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var c = await context.Cuestionarios.FindAsync(id);
        if (c is null) return;
        c.Activo = false;
        await context.SaveChangesAsync();
    }

    public async Task<RespuestaCuestionario?> GetRespuestaAsync(int cuestionarioId, int inscripcionId)
    {
        var respuestas = await context.RespuestasCuestionario
            .Where(r => r.CuestionarioId == cuestionarioId && r.InscripcionId == inscripcionId)
            .ToListAsync();

        if (!respuestas.Any()) return null;

        var aprobada = respuestas.FirstOrDefault(r => r.Aprobado);
        return aprobada ?? respuestas.OrderByDescending(r => r.FechaRespuesta).First();
    }

    public async Task<List<RespuestaCuestionario>> GetRespuestasAsync(int cuestionarioId, int inscripcionId) =>
        await context.RespuestasCuestionario
            .Where(r => r.CuestionarioId == cuestionarioId && r.InscripcionId == inscripcionId)
            .OrderBy(r => r.FechaRespuesta)
            .ToListAsync();

    public async Task<RespuestaCuestionario> SaveRespuestaAsync(RespuestaCuestionario respuesta)
    {
        // Separar los hijos ANTES de agregar el padre al contexto.
        var hijos = respuesta.Respuestas.ToList();
        respuesta.Respuestas = [];

        context.RespuestasCuestionario.Add(respuesta);
        await context.SaveChangesAsync();

        foreach (var r in hijos)
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO RespuestasPregunta (RespuestaCuestionarioId, PreguntaId, OpcionElegidaId, Activo) VALUES ({0}, {1}, {2}, 1)",
                respuesta.Id, r.PreguntaId, r.OpcionElegidaId);
        }

        return respuesta;
    }

    public async Task<List<int>> GetCapacitacionesAprobadasPorColaboradorAsync(int colaboradorId)
    {
        var respuestas = await context.RespuestasCuestionario
            .Include(r => r.Cuestionario)
            .Where(r => r.Inscripcion.ColaboradorId == colaboradorId)
            .ToListAsync();

        var completadas = new List<int>();

        var grupos = respuestas.GroupBy(r => r.CuestionarioId);
        foreach (var grupo in grupos)
        {
            var intentosRealizados = grupo.Count();
            var cuestionario = grupo.First().Cuestionario;
            var intentosPermitidos = cuestionario?.IntentosPermitidos ?? 1;

            var aprobado = grupo.Any(r => r.Aprobado);
            if (aprobado || intentosRealizados >= intentosPermitidos)
            {
                completadas.Add(grupo.First().Inscripcion.CapacitacionId);
            }
        }

        return completadas.Distinct().ToList();
    }

    public async Task<int> ContarRespuestasCapacitacionAsync(int capacitacionId)
    {
        var cuestionario = await context.Cuestionarios
            .FirstOrDefaultAsync(c => c.CapacitacionId == capacitacionId);

        if (cuestionario is null) return 0;

        var intentosPermitidos = cuestionario.IntentosPermitidos;

        var respuestas = await context.RespuestasCuestionario
            .Where(r => r.CuestionarioId == cuestionario.Id && r.Inscripcion.Activo)
            .ToListAsync();

        var totalFinalizados = respuestas
            .GroupBy(r => r.InscripcionId)
            .Count(grupo => grupo.Any(r => r.Aprobado) || grupo.Count() >= intentosPermitidos);

        return totalFinalizados;
    }
}
