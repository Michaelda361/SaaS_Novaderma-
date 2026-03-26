using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Areas;

namespace TalentManagement.Application.Services;

public class AreaService(IAreaRepository repository)
{
    public async Task<List<AreaDto>> GetAllAsync()
    {
        var areas = await repository.GetAllAsync();
        return areas.Select(MapToDto).ToList();
    }

    public async Task<AreaDto?> GetByIdAsync(int id)
    {
        var area = await repository.GetByIdAsync(id);
        return area is null ? null : MapToDto(area);
    }

    public async Task<AreaDto> CreateAsync(CreateAreaDto dto)
    {
        var area = new Area
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            JefeId = dto.JefeId
        };
        var created = await repository.CreateAsync(area);
        return MapToDto(await repository.GetByIdAsync(created.Id) ?? created);
    }

    public async Task<AreaDto?> UpdateAsync(int id, CreateAreaDto dto)
    {
        var area = await repository.GetByIdAsync(id);
        if (area is null) return null;
        area.Nombre = dto.Nombre;
        area.Descripcion = dto.Descripcion;
        area.JefeId = dto.JefeId;
        await repository.UpdateAsync(area);
        return MapToDto(await repository.GetByIdAsync(id)!);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var area = await repository.GetByIdAsync(id);
        if (area is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    private static AreaDto MapToDto(Area a) => new()
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Descripcion = a.Descripcion,
        JefeId = a.JefeId,
        JefeNombre = a.Jefe is null ? null : $"{a.Jefe.Nombre} {a.Jefe.Apellido}",
        TotalCargos = a.Cargos.Count,
        TotalColaboradores = a.Colaboradores.Count
    };
}
