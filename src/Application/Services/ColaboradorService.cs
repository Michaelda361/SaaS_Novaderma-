using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Colaboradores;

namespace TalentManagement.Application.Services;

public class ColaboradorService(IColaboradorRepository repository)
{
    public async Task<IEnumerable<ColaboradorDto>> GetAllAsync()
    {
        var colaboradores = await repository.GetAllAsync();
        return colaboradores.Select(MapToDto);
    }

    public async Task<ColaboradorDto?> GetByIdAsync(int id)
    {
        var colaborador = await repository.GetByIdAsync(id);
        return colaborador is null ? null : MapToDto(colaborador);
    }

    public async Task<ColaboradorDto> CreateAsync(CreateColaboradorDto dto)
    {
        var colaborador = new Colaborador
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Email = dto.Email,
            Telefono = dto.Telefono,
            FechaIngreso = dto.FechaIngreso,
            AreaId = dto.AreaId,
            CargoId = dto.CargoId,
            SupervisorId = dto.SupervisorId
        };

        var created = await repository.CreateAsync(colaborador);
        return MapToDto(created);
    }

    public async Task<ColaboradorDto?> UpdateAsync(int id, UpdateColaboradorDto dto)
    {
        if (!await repository.ExistsAsync(id)) return null;

        var colaborador = await repository.GetByIdAsync(id);
        colaborador!.Nombre = dto.Nombre;
        colaborador.Apellido = dto.Apellido;
        colaborador.Email = dto.Email;
        colaborador.Telefono = dto.Telefono;
        colaborador.AreaId = dto.AreaId;
        colaborador.CargoId = dto.CargoId;
        colaborador.SupervisorId = dto.SupervisorId;

        var updated = await repository.UpdateAsync(colaborador);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        if (!await repository.ExistsAsync(id)) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    private static ColaboradorDto MapToDto(Colaborador c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Apellido = c.Apellido,
        Email = c.Email,
        Telefono = c.Telefono,
        FechaIngreso = c.FechaIngreso,
        AreaNombre = c.Area?.Nombre ?? string.Empty,
        CargoNombre = c.Cargo?.Nombre ?? string.Empty,
        SupervisorNombre = c.Supervisor is null ? null : $"{c.Supervisor.Nombre} {c.Supervisor.Apellido}"
    };
}
