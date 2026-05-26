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
        var query = Documentos().Where(d => d.ListadoMaestroId == listadoId);

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

        return await query.OrderBy(d => d.Nombre).ToListAsync();
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
            .FirstOrDefaultAsync(d => d.ListadoMaestroId == listadoId && d.Codigo == codigo);

    public async Task<DocumentoControl> CreateDocumentoAsync(DocumentoControl documento)
    {
        context.DocumentosControl.Add(documento);
        await context.SaveChangesAsync();
        return documento;
    }

    public async Task<DocumentoControl> UpdateDocumentoAsync(DocumentoControl documento)
    {
        context.DocumentosControl.Update(documento);
        await context.SaveChangesAsync();
        return documento;
    }

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
}
