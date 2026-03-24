using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Shared.DTOs.Recursos;

namespace TalentManagement.Application.Services;

public class RecursoService(IRecursoRepository repository)
{
    public async Task<IEnumerable<RecursoDto>> GetByCapacitacionAsync(int capacitacionId)
    {
        var items = await repository.GetByCapacitacionAsync(capacitacionId);
        return items.Select(MapToDto);
    }

    public async Task<RecursoDto> CreateAsync(CreateRecursoDto dto)
    {
        var recurso = new RecursoCapacitacion
        {
            Titulo = dto.Titulo,
            Url = dto.Url,
            Tipo = Enum.TryParse<TipoRecurso>(dto.Tipo, out var t) ? t : TipoRecurso.Enlace,
            Descripcion = dto.Descripcion,
            Orden = dto.Orden,
            CapacitacionId = dto.CapacitacionId
        };
        var created = await repository.CreateAsync(recurso);
        return MapToDto(created);
    }

    public async Task<RecursoDto?> UpdateAsync(int id, CreateRecursoDto dto)
    {
        var recurso = await repository.GetByIdAsync(id);
        if (recurso is null) return null;

        recurso.Titulo = dto.Titulo;
        recurso.Url = dto.Url;
        recurso.Tipo = Enum.TryParse<TipoRecurso>(dto.Tipo, out var t) ? t : TipoRecurso.Enlace;
        recurso.Descripcion = dto.Descripcion;
        recurso.Orden = dto.Orden;

        var updated = await repository.UpdateAsync(recurso);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id) => await repository.DeleteAsync(id);

    private static RecursoDto MapToDto(RecursoCapacitacion r) => new()
    {
        Id = r.Id,
        Titulo = r.Titulo,
        Url = r.Url,
        Tipo = r.Tipo.ToString(),
        Descripcion = r.Descripcion,
        Orden = r.Orden,
        CapacitacionId = r.CapacitacionId
    };
}
