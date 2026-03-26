using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Cargos;

namespace TalentManagement.Application.Services;

public class CargoService(ICargoRepository repository)
{
    public async Task<List<CargoDto>> GetAllAsync()
    {
        var cargos = await repository.GetAllAsync();
        return cargos.Select(MapToDto).ToList();
    }

    public async Task<List<CargoDto>> GetByAreaAsync(int areaId)
    {
        var cargos = await repository.GetByAreaAsync(areaId);
        return cargos.Select(MapToDto).ToList();
    }

    public async Task<CargoDto?> GetByIdAsync(int id)
    {
        var cargo = await repository.GetByIdAsync(id);
        return cargo is null ? null : MapToDto(cargo);
    }

    public async Task<CargoDto> CreateAsync(CreateCargoDto dto)
    {
        var cargo = new Cargo
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            AreaId = dto.AreaId
        };
        var created = await repository.CreateAsync(cargo);
        return MapToDto(await repository.GetByIdAsync(created.Id) ?? created);
    }

    public async Task<CargoDto?> UpdateAsync(int id, CreateCargoDto dto)
    {
        var cargo = await repository.GetByIdAsync(id);
        if (cargo is null) return null;
        cargo.Nombre = dto.Nombre;
        cargo.Descripcion = dto.Descripcion;
        cargo.AreaId = dto.AreaId;
        var updated = await repository.UpdateAsync(cargo);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cargo = await repository.GetByIdAsync(id);
        if (cargo is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    private static CargoDto MapToDto(Cargo c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        AreaId = c.AreaId,
        AreaNombre = c.Area?.Nombre ?? string.Empty,
        TotalColaboradores = c.Colaboradores.Count
    };
}
