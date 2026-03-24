using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class DocumentoRepository(AppDbContext context) : IDocumentoRepository
{
    private IQueryable<Documento> WithBase() =>
        context.Documentos.Include(d => d.Area);

    private IQueryable<Documento> WithDetalles() =>
        context.Documentos
            .Include(d => d.Area)
            .Include(d => d.Versiones)
            .Include(d => d.FlujoAprobacion).ThenInclude(f => f.Colaborador)
            .Include(d => d.Propuestas).ThenInclude(p => p.Colaborador)
            .Include(d => d.Propuestas).ThenInclude(p => p.Area);

    public async Task<IEnumerable<Documento>> GetAllAsync() =>
        await WithBase().AsNoTracking().ToListAsync();

    public async Task<IEnumerable<Documento>> GetPublicadosAsync() =>
        await WithBase().AsNoTracking()
            .Where(d => d.Estado == EstadoDocumento.Publicado)
            .ToListAsync();

    public async Task<Documento?> GetByIdAsync(int id) =>
        await WithBase().FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Documento?> GetByIdConDetallesAsync(int id) =>
        await WithDetalles().FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Documento> CreateAsync(Documento documento)
    {
        context.Documentos.Add(documento);
        await context.SaveChangesAsync();
        return documento;
    }

    public async Task<Documento> UpdateAsync(Documento documento)
    {
        context.Documentos.Update(documento);
        await context.SaveChangesAsync();
        return documento;
    }

    public async Task DeleteAsync(int id)
    {
        var doc = await context.Documentos.FindAsync(id);
        if (doc is null) return;
        doc.Activo = false;
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<PropuestaModificacion>> GetPropuestasPendientesPorAreaAsync(
        int areaId) =>
        await context.PropuestasModificacion
            .Include(p => p.Colaborador)
            .Include(p => p.Area)
            .Include(p => p.Documento)
            .Where(p => p.AreaId == areaId &&
                        p.EstadoPropuesta == EstadoPropuesta.PendienteRevision)
            .AsNoTracking()
            .ToListAsync();

    public async Task<int> CountPropuestasPendientesPorAreaAsync(int areaId) =>
        await context.PropuestasModificacion
            .CountAsync(p => p.AreaId == areaId &&
                             p.EstadoPropuesta == EstadoPropuesta.PendienteRevision);

    public async Task<PropuestaModificacion?> GetPropuestaByIdAsync(int id) =>
        await context.PropuestasModificacion
            .Include(p => p.Colaborador)
            .Include(p => p.Area)
            .Include(p => p.Documento)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PropuestaModificacion> CreatePropuestaAsync(PropuestaModificacion p)
    {
        context.PropuestasModificacion.Add(p);
        await context.SaveChangesAsync();
        return p;
    }

    public async Task<PropuestaModificacion> UpdatePropuestaAsync(PropuestaModificacion p)
    {
        context.PropuestasModificacion.Update(p);
        await context.SaveChangesAsync();
        return p;
    }

    public async Task CreateVersionAsync(VersionDocumento version)
    {
        context.VersionesDocumento.Add(version);
        await context.SaveChangesAsync();
    }

    public async Task CreateFlujoAsync(FlujoAprobacionDoc flujo)
    {
        context.FlujosAprobacionDoc.Add(flujo);
        await context.SaveChangesAsync();
    }
}
