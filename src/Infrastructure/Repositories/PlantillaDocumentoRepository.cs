using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class PlantillaDocumentoRepository(AppDbContext context) : IPlantillaDocumentoRepository
{
    // Para listados: excluye ArchivoDocx y ContenidoHtml (datos pesados)
    private IQueryable<PlantillaDocumento> WithAreasNoBytes() =>
        context.PlantillasDocumento
            .Include(p => p.Areas).ThenInclude(a => a.Area)
            .Select(p => new PlantillaDocumento
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                TipoPlantilla = p.TipoPlantilla,
                ContenidoHtml = null,
                ArchivoDocxLegacy = null,   // excluir binario pesado en listados
                DocxFileKey = p.DocxFileKey,
                FirmaImagenBase64 = p.FirmaImagenBase64,
                NombreFirmante = p.NombreFirmante,
                CargoFirmante = p.CargoFirmante,
                AplicaTodasAreas = p.AplicaTodasAreas,
                VariablesEditables = p.VariablesEditables,
                Activo = p.Activo,
                Areas = p.Areas
            });

    // Para generación: incluye todo
    private IQueryable<PlantillaDocumento> WithAreasFull() =>
        context.PlantillasDocumento
            .Include(p => p.Areas).ThenInclude(a => a.Area);

    public async Task<IEnumerable<PlantillaDocumento>> GetAllAsync() =>
        await WithAreasNoBytes().AsNoTracking().ToListAsync();

    public async Task<IEnumerable<PlantillaDocumento>> GetByAreaAsync(int areaId) =>
        await WithAreasNoBytes()
            .Where(p => p.AplicaTodasAreas || p.Areas.Any(a => a.AreaId == areaId))
            .AsNoTracking()
            .ToListAsync();

    public async Task<PlantillaDocumento?> GetByIdAsync(int id) =>
        await WithAreasFull().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PlantillaDocumento> CreateAsync(PlantillaDocumento plantilla)
    {
        context.PlantillasDocumento.Add(plantilla);
        await context.SaveChangesAsync();
        return await WithAreasFull().FirstAsync(p => p.Id == plantilla.Id);
    }

    public async Task<PlantillaDocumento> UpdateAsync(PlantillaDocumento plantilla)
    {
        var areasExistentes = await context.PlantillaDocumentoAreas
            .Where(a => a.PlantillaDocumentoId == plantilla.Id)
            .ToListAsync();
        context.PlantillaDocumentoAreas.RemoveRange(areasExistentes);

        context.PlantillasDocumento.Update(plantilla);
        await context.SaveChangesAsync();
        return await WithAreasFull().FirstAsync(p => p.Id == plantilla.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var p = await context.PlantillasDocumento.FindAsync(id);
        if (p is null) return;
        p.Activo = false;
        await context.SaveChangesAsync();
    }

    public async Task<SolicitudDocumento> CreateSolicitudAsync(SolicitudDocumento solicitud)
    {
        context.SolicitudesDocumento.Add(solicitud);
        await context.SaveChangesAsync();
        return solicitud;
    }

    public async Task<SolicitudDocumento?> GetSolicitudByIdAsync(int id) =>
        await context.SolicitudesDocumento
            .Include(s => s.PlantillaDocumento)
            .Include(s => s.Colaborador)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<SolicitudDocumento> UpdateSolicitudAsync(SolicitudDocumento solicitud)
    {
        context.SolicitudesDocumento.Update(solicitud);
        await context.SaveChangesAsync();
        return solicitud;
    }

    public async Task<IEnumerable<SolicitudDocumento>> GetSolicitudesByColaboradorAsync(int colaboradorId)
    {
        var rows = await context.SolicitudesDocumento
            .Include(s => s.PlantillaDocumento)
            .Include(s => s.Colaborador)
            .Where(s => s.ColaboradorId == colaboradorId)
            .OrderByDescending(s => s.FechaSolicitud)
            .Select(s => new
            {
                s.Id, s.PlantillaDocumentoId, s.PlantillaDocumento,
                s.ColaboradorId, s.Colaborador, s.FechaSolicitud,
                s.Estado, s.ComentarioAdmin, s.FechaResolucion, s.Activo,
                s.NotificadoColaborador,
                TienePdf = s.PdfBytes != null,
            })
            .AsNoTracking().ToListAsync();

        return rows.Select(s => new SolicitudDocumento
        {
            Id = s.Id, PlantillaDocumentoId = s.PlantillaDocumentoId,
            PlantillaDocumento = s.PlantillaDocumento, ColaboradorId = s.ColaboradorId,
            Colaborador = s.Colaborador, FechaSolicitud = s.FechaSolicitud,
            Estado = s.Estado, ComentarioAdmin = s.ComentarioAdmin,
            FechaResolucion = s.FechaResolucion, Activo = s.Activo,
            NotificadoColaborador = s.NotificadoColaborador,
            PdfBytes = s.TienePdf ? [] : null,
        });
    }

    public async Task<IEnumerable<SolicitudDocumento>> GetTodasSolicitudesAsync()
    {
        var rows = await context.SolicitudesDocumento
            .Include(s => s.PlantillaDocumento)
            .Include(s => s.Colaborador)
            .OrderByDescending(s => s.FechaSolicitud)
            .Select(s => new
            {
                s.Id, s.PlantillaDocumentoId, s.PlantillaDocumento,
                s.ColaboradorId, s.Colaborador, s.FechaSolicitud,
                s.Estado, s.ComentarioAdmin, s.FechaResolucion, s.Activo,
                TienePdf = s.PdfBytes != null,
            })
            .AsNoTracking().ToListAsync();

        return rows.Select(s => new SolicitudDocumento
        {
            Id = s.Id, PlantillaDocumentoId = s.PlantillaDocumentoId,
            PlantillaDocumento = s.PlantillaDocumento, ColaboradorId = s.ColaboradorId,
            Colaborador = s.Colaborador, FechaSolicitud = s.FechaSolicitud,
            Estado = s.Estado, ComentarioAdmin = s.ComentarioAdmin,
            FechaResolucion = s.FechaResolucion, Activo = s.Activo,
            PdfBytes = s.TienePdf ? [] : null,
        });
    }

    public async Task<IEnumerable<SolicitudDocumento>> GetSolicitudesPendientesAsync()
    {
        var rows = await context.SolicitudesDocumento
            .Include(s => s.PlantillaDocumento)
            .Include(s => s.Colaborador)
            .Where(s => s.Estado == Domain.Enums.EstadoSolicitud.Pendiente)
            .OrderByDescending(s => s.FechaSolicitud)
            .Select(s => new
            {
                s.Id, s.PlantillaDocumentoId, s.PlantillaDocumento,
                s.ColaboradorId, s.Colaborador, s.FechaSolicitud,
                s.Estado, s.ComentarioAdmin, s.FechaResolucion, s.Activo,
                TienePdf = s.PdfBytes != null,
            })
            .AsNoTracking().ToListAsync();

        return rows.Select(s => new SolicitudDocumento
        {
            Id = s.Id, PlantillaDocumentoId = s.PlantillaDocumentoId,
            PlantillaDocumento = s.PlantillaDocumento, ColaboradorId = s.ColaboradorId,
            Colaborador = s.Colaborador, FechaSolicitud = s.FechaSolicitud,
            Estado = s.Estado, ComentarioAdmin = s.ComentarioAdmin,
            FechaResolucion = s.FechaResolucion, Activo = s.Activo,
            PdfBytes = s.TienePdf ? [] : null,
        });
    }

    public Task<int> CountPendientesAsync() =>
        context.SolicitudesDocumento
            .CountAsync(s => s.Estado == Domain.Enums.EstadoSolicitud.Pendiente);

    public Task<bool> ExisteSolicitudPendienteAsync(int plantillaId, int colaboradorId) =>
        context.SolicitudesDocumento.AnyAsync(s =>
            s.PlantillaDocumentoId == plantillaId &&
            s.ColaboradorId == colaboradorId &&
            s.Estado == EstadoSolicitud.Pendiente);

    public async Task MarcarSolicitudesComoVistaAsync(int colaboradorId)
    {
        await context.SolicitudesDocumento
            .Where(s => s.ColaboradorId == colaboradorId && !s.NotificadoColaborador)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.NotificadoColaborador, true));
    }
}
