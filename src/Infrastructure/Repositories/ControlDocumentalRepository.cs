using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class ControlDocumentalRepository(AppDbContext context) : IControlDocumentalRepository
{
    private IQueryable<ListadoMaestro> Listados() =>
        context.ListadosMaestros.AsNoTracking();

    private IQueryable<DocumentoControl> Documentos() =>
        context.DocumentosControl
            .Include(d => d.ListadoMaestro)
            .Include(d => d.Area)
            .AsNoTracking();

    public async Task<IEnumerable<ListadoMaestro>> GetListadosAsync() =>
        await Listados().OrderBy(l => l.Nombre).ToListAsync();

    public async Task<ListadoMaestro?> GetListadoByIdAsync(int id) =>
        await context.ListadosMaestros
            .Include(l => l.Campos)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<IEnumerable<DocumentoControlCampoDefinicion>> GetCamposPorListadoAsync(int listadoId) =>
        await context.DocumentoControlCampoDefiniciones
            .Where(c => c.ListadoMaestroId == listadoId)
            .OrderBy(c => c.Orden)
            .ToListAsync();

    public async Task<DocumentoControlCampoDefinicion> CreateCampoAsync(DocumentoControlCampoDefinicion campo)
    {
        context.DocumentoControlCampoDefiniciones.Add(campo);
        await context.SaveChangesAsync();
        return campo;
    }

    public async Task<DocumentoControlCampoDefinicion> UpdateCampoAsync(DocumentoControlCampoDefinicion campo)
    {
        context.DocumentoControlCampoDefiniciones.Update(campo);
        await context.SaveChangesAsync();
        return campo;
    }

    public async Task DeleteCampoAsync(DocumentoControlCampoDefinicion campo)
    {
        campo.Activo = false;
        context.DocumentoControlCampoDefiniciones.Update(campo);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DocumentoControl>> GetDocumentosAsync(
        int listadoId, int? areaId, string? busqueda, string? codigo,
        string? proceso, string? estado)
    {
        var query = Documentos().Where(d => d.ListadoMaestroId == listadoId && d.Estado == "Vigente");

        if (areaId.HasValue)
            query = query.Where(d => d.AreaId == areaId);

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(d => d.Nombre.Contains(busqueda) || d.Codigo.Contains(busqueda));

        if (!string.IsNullOrWhiteSpace(codigo))
            query = query.Where(d => d.Codigo.Contains(codigo));

        if (!string.IsNullOrWhiteSpace(proceso))
            query = query.Where(d => d.ProcesoResponsable.Contains(proceso));

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(d => d.Estado.Contains(estado));

        var results = await query
            .OrderByDescending(d => d.FechaPublicacion ?? d.FechaDocumento)
            .ThenByDescending(d => d.Id)
            .ToListAsync();
        return results;
    }

    public async Task<DocumentoControl?> GetDocumentoByIdAsync(int id) =>
        await context.DocumentosControl
            .Include(d => d.ListadoMaestro)
            .Include(d => d.Area)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<DocumentoControl?> GetDocumentoByCodigoAsync(int listadoId, string codigo) =>
        await context.DocumentosControl
            .Include(d => d.ListadoMaestro)
            .Include(d => d.Area)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.ListadoMaestroId == listadoId && d.Codigo == codigo);

    public async Task<DocumentoControl?> GetDocumentoByCodigoYNombreAsync(int listadoId, string codigo, string nombre) =>
        await context.DocumentosControl
            .Include(d => d.ListadoMaestro)
            .Include(d => d.Area)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.ListadoMaestroId == listadoId && d.Codigo == codigo && d.Nombre == nombre);

    public async Task<DocumentoControl> CreateDocumentoAsync(DocumentoControl documento)
    {
        context.DocumentosControl.Add(documento);
        await context.SaveChangesAsync();
        return documento;
    }

    public async Task<DocumentoControl> UpdateDocumentoAsync(DocumentoControl documento)
    {
        var trackedEntry = context.ChangeTracker.Entries<DocumentoControl>()
            .FirstOrDefault(e => e.Entity.Id == documento.Id);

        if (trackedEntry != null)
        {
            if (trackedEntry.Entity != documento)
            {
                // Copy values to the tracked instance
                context.Entry(trackedEntry.Entity).CurrentValues.SetValues(documento);
                
                // Ensure Activo and foreign keys are explicitly updated
                trackedEntry.Entity.Activo = documento.Activo;
                
                documento = trackedEntry.Entity;
            }
            else
            {
                trackedEntry.State = EntityState.Modified;
            }
        }
        else
        {
            context.DocumentosControl.Update(documento);
        }

        await context.SaveChangesAsync();
        return documento;
    }

    public async Task<bool> ExisteSolicitudCambioPendienteAsync(int documentoControlId, int colaboradorId) =>
        await context.SolicitudesCambioDocumentoControl
            .AnyAsync(s => s.DocumentoControlId == documentoControlId
                && s.SolicitanteId == colaboradorId
                && s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.PendienteRevision);

    public async Task<SolicitudCambioDocumentoControl> CreateSolicitudCambioAsync(SolicitudCambioDocumentoControl solicitud)
    {
        context.SolicitudesCambioDocumentoControl.Add(solicitud);
        await context.SaveChangesAsync();
        return solicitud;
    }

    public async Task<SolicitudCambioDocumentoControl> UpdateSolicitudCambioAsync(SolicitudCambioDocumentoControl solicitud)
    {
        context.SolicitudesCambioDocumentoControl.Update(solicitud);
        await context.SaveChangesAsync();
        return solicitud;
    }

    public async Task<SolicitudCambioDocumentoControl?> GetSolicitudCambioByIdAsync(int id) =>
        await context.SolicitudesCambioDocumentoControl
            .Include(s => s.DocumentoControl)
            .Include(s => s.Solicitante)
            .Include(s => s.Editor)
            .Include(s => s.Aprobador)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<SolicitudCambioDocumentoControl>> GetSolicitudesPorDocumentoAsync(int documentoId)
    {
        var targetDoc = await context.DocumentosControl
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == documentoId);

        if (targetDoc == null)
            return Enumerable.Empty<SolicitudCambioDocumentoControl>();

        int parentId = targetDoc.DocumentoOriginalId ?? targetDoc.Id;

        return await context.SolicitudesCambioDocumentoControl
            .IgnoreQueryFilters()
            .Include(s => s.DocumentoControl)
            .Include(s => s.Solicitante)
            .Include(s => s.Editor)
            .Include(s => s.Aprobador)
            .Where(s => (s.DocumentoControl.ListadoMaestroId == targetDoc.ListadoMaestroId && s.DocumentoControl.Codigo == targetDoc.Codigo)
                     || s.DocumentoControlId == parentId || s.DocumentoControl.DocumentoOriginalId == parentId)
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();
    }

    public async Task<IEnumerable<SolicitudCambioDocumentoControl>> GetSolicitudesCambioPendientesAsync() =>
        await context.SolicitudesCambioDocumentoControl
            .IgnoreQueryFilters()
            .Include(s => s.DocumentoControl)
            .Include(s => s.Solicitante)
            .Include(s => s.Aprobador)
            .Where(s => s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.PendienteRevision
                || s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.EnEdicion
                || s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.PendienteAprobacion)
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();

    public async Task<IEnumerable<SolicitudCambioDocumentoControl>> GetSolicitudesCambioPendientesPorAreaAsync(int areaId) =>
        await context.SolicitudesCambioDocumentoControl
            .IgnoreQueryFilters()
            .Include(s => s.DocumentoControl)
            .Include(s => s.Solicitante)
            .Include(s => s.Aprobador)
            .Where(s => s.DocumentoControl.AreaId == areaId
                && (s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.PendienteRevision
                    || s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.EnEdicion
                    || s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.PendienteAprobacion))
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();

    public async Task<int> CountSolicitudesCambioPendientesPorAreaAsync(int areaId) =>
        await context.SolicitudesCambioDocumentoControl
            .IgnoreQueryFilters()
            .Where(s => (s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.PendienteRevision
                || s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.EnEdicion
                || s.EstadoPropuesta == Domain.Enums.EstadoPropuesta.PendienteAprobacion)
                && s.DocumentoControl.AreaId == areaId)
            .CountAsync();

    public async Task<ListadoMaestro?> GetListadoByNombreAsync(string nombre) =>
        await context.ListadosMaestros
            .Include(l => l.Campos)
            .FirstOrDefaultAsync(l => l.Nombre == nombre);

    public async Task<ListadoMaestro> CreateListadoAsync(ListadoMaestro listado)
    {
        context.ListadosMaestros.Add(listado);
        await context.SaveChangesAsync();
        return listado;
    }

    public async Task<ListadoMaestro> UpdateListadoAsync(ListadoMaestro listado)
    {
        context.ListadosMaestros.Update(listado);
        await context.SaveChangesAsync();
        return listado;
    }

    public async Task<bool> DeleteListadoAsync(int id)
    {
        var listado = await context.ListadosMaestros
            .Include(l => l.Documentos)
            .Include(l => l.Campos)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listado is null)
        {
            return false;
        }

        listado.Activo = false;

        foreach (var documento in listado.Documentos)
        {
            documento.Activo = false;
        }

        foreach (var campo in listado.Campos)
        {
            campo.Activo = false;
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ListadoMaestroPermiso>> GetPermisosPorListadoAsync(int listadoId)
    {
        var list = await context.ListadoMaestroPermisos
            .Include(p => p.Colaborador)
            .Include(p => p.Area)
            .Where(p => p.ListadoMaestroId == listadoId)
            .AsNoTracking()
            .ToListAsync();

        return list.OrderBy(p => p.Colaborador != null 
            ? $"{p.Colaborador.Nombre} {p.Colaborador.Apellido}" 
            : p.Area?.Nombre ?? string.Empty);
    }

    public async Task<IEnumerable<ListadoMaestroPermiso>> CreatePermisosAsync(IEnumerable<ListadoMaestroPermiso> permisos)
    {
        context.ListadoMaestroPermisos.AddRange(permisos);
        await context.SaveChangesAsync();
        return permisos;
    }

    public async Task DeletePermisosPorListadoAsync(int listadoId)
    {
        var permisos = await context.ListadoMaestroPermisos
            .Where(p => p.ListadoMaestroId == listadoId)
            .ToListAsync();
        context.ListadoMaestroPermisos.RemoveRange(permisos);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ListadoMaestroPermiso>> GetPermisosPorColaboradorAsync(int colaboradorId)
    {
        var colaborador = await context.Colaboradores.FindAsync(colaboradorId);
        if (colaborador is null)
            return Enumerable.Empty<ListadoMaestroPermiso>();

        return await context.ListadoMaestroPermisos
            .Include(p => p.Colaborador)
            .Include(p => p.Area)
            .Where(p => p.ColaboradorId == colaboradorId || (p.AreaId.HasValue && p.AreaId == colaborador.AreaId))
            .OrderBy(p => p.ListadoMaestroId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task DeleteDocumentoAsync(DocumentoControl documento)
    {
        context.DocumentosControl.Remove(documento);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DocumentoControl>> GetDocumentosIgnoreFiltersAsync(int documentoId)
    {
        var targetDoc = await context.DocumentosControl
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == documentoId);

        if (targetDoc == null)
            return Enumerable.Empty<DocumentoControl>();

        int parentId = targetDoc.DocumentoOriginalId ?? targetDoc.Id;

        var list = await context.DocumentosControl
            .IgnoreQueryFilters()
            .Include(d => d.ListadoMaestro)
            .Include(d => d.Area)
            .Include(d => d.Solicitante)
            .Include(d => d.Editor)
            .Include(d => d.Aprobador)
            .Where(d => (d.ListadoMaestroId == targetDoc.ListadoMaestroId && d.Codigo == targetDoc.Codigo)
                     || d.Id == parentId || d.DocumentoOriginalId == parentId)
            .ToListAsync();

        return list
            .OrderByDescending(d => d.Estado == "Vigente")
            .ThenByDescending(d => d.Id);
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await action();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}
