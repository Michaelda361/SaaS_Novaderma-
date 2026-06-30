using Microsoft.EntityFrameworkCore;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.Infrastructure.Repositories;

public class ColaboradorRepository(AppDbContext context) : IColaboradorRepository
{
    public async Task<IEnumerable<Colaborador>> GetAllAsync() =>
        await context.Colaboradores
            .Include(c => c.Area)
            .Include(c => c.Cargo)
            .Include(c => c.Supervisor)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Colaborador?> GetByIdAsync(int id) =>
        await context.Colaboradores
            .Include(c => c.Area)
            .Include(c => c.Cargo)
            .Include(c => c.Supervisor)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Colaborador?> GetByEmailAsync(string email) =>
        await context.Colaboradores
            .Include(c => c.Area)
            .Include(c => c.Cargo)
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());

    public async Task<List<ColaboradorCampoDefinicion>> GetCamposDefinicionAsync() =>
        await context.ColaboradorCampoDefiniciones
            .OrderBy(c => c.Orden)
            .AsNoTracking()
            .ToListAsync();

    public async Task<ColaboradorCampoDefinicion?> GetCampoDefinicionAsync(int id) =>
        await context.ColaboradorCampoDefiniciones
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<ColaboradorCampoDefinicion> CreateCampoDefinicionAsync(ColaboradorCampoDefinicion campo)
    {
        context.ColaboradorCampoDefiniciones.Add(campo);
        await context.SaveChangesAsync();
        return campo;
    }

    public async Task<ColaboradorCampoDefinicion> UpdateCampoDefinicionAsync(ColaboradorCampoDefinicion campo)
    {
        context.ColaboradorCampoDefiniciones.Update(campo);
        await context.SaveChangesAsync();
        return campo;
    }

    public async Task DeleteCampoDefinicionAsync(int id)
    {
        var campo = await context.ColaboradorCampoDefiniciones.FindAsync(id);
        if (campo is null) return;
        campo.Activo = false;
        await context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string?>> GetValoresPorColaboradorAsync(int colaboradorId)
    {
        return await context.ColaboradorCampoValores
            .Where(v => v.ColaboradorId == colaboradorId)
            .Include(v => v.ColaboradorCampoDefinicion)
            .AsNoTracking()
            .ToDictionaryAsync(v => v.ColaboradorCampoDefinicion.CampoClave,
                               v => v.Valor);
    }

    public async Task SetValoresAdicionalesAsync(int colaboradorId, Dictionary<string, string?> valores)
    {
        var definiciones = await context.ColaboradorCampoDefiniciones
            .Where(c => c.Activo)
            .ToListAsync();

        var existentes = await context.ColaboradorCampoValores
            .Where(v => v.ColaboradorId == colaboradorId)
            .ToListAsync();

        context.ColaboradorCampoValores.RemoveRange(existentes);

        var nuevosValores = valores
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .Select(kvp => new ColaboradorCampoValor
            {
                ColaboradorId = colaboradorId,
                ColaboradorCampoDefinicionId = definiciones
                    .FirstOrDefault(d => d.CampoClave.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    ?.Id ?? 0,
                Valor = kvp.Value
            })
            .Where(v => v.ColaboradorCampoDefinicionId > 0)
            .ToList();

        if (nuevosValores.Count > 0)
            context.ColaboradorCampoValores.AddRange(nuevosValores);

        await context.SaveChangesAsync();
    }

    public async Task<Colaborador> CreateAsync(Colaborador colaborador)
    {
        context.Colaboradores.Add(colaborador);
        await context.SaveChangesAsync();
        return await GetByIdAsync(colaborador.Id) ?? colaborador;
    }

    public async Task<Colaborador> UpdateAsync(Colaborador colaborador)
    {
        context.Colaboradores.Update(colaborador);
        await context.SaveChangesAsync();
        return await GetByIdAsync(colaborador.Id) ?? colaborador;
    }

    public async Task DeleteAsync(int id)
    {
        var colaborador = await context.Colaboradores.FindAsync(id);
        if (colaborador is null) return;
        // Soft delete
        colaborador.Activo = false;
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id) =>
        await context.Colaboradores.AnyAsync(c => c.Id == id);

    public async Task<bool> EsJefeDeAreaAsync(int colaboradorId) =>
        await context.Areas.AnyAsync(a => a.JefeId == colaboradorId);

    public async Task<IEnumerable<Colaborador>> GetInactivosAsync() =>
        await context.Colaboradores
            .IgnoreQueryFilters()
            .Where(c => !c.Activo)
            .Include(c => c.Area)
            .Include(c => c.Cargo)
            .AsNoTracking()
            .ToListAsync();

    public async Task RestaurarAsync(int id)
    {
        var colaborador = await context.Colaboradores
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id);
        if (colaborador is null) return;
        colaborador.Activo = true;
        await context.SaveChangesAsync();
    }
}
