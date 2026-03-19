using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Capacitaciones;

namespace TalentManagement.Application.Services;

public class CapacitacionService(ICapacitacionRepository repository)
{
    public async Task<IEnumerable<CapacitacionDto>> GetAllAsync()
    {
        var capacitaciones = await repository.GetAllAsync();
        return capacitaciones.Select(MapToDto);
    }

    public async Task<CapacitacionDto?> GetByIdAsync(int id)
    {
        var cap = await repository.GetByIdAsync(id);
        return cap is null ? null : MapToDto(cap);
    }

    public async Task<CapacitacionDto> CreateAsync(CreateCapacitacionDto dto)
    {
        var capacitacion = new Capacitacion
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            DuracionHoras = dto.DuracionHoras,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin
        };

        var created = await repository.CreateAsync(capacitacion);
        return MapToDto(created);
    }

    public async Task<CapacitacionDto?> UpdateAsync(int id, CreateCapacitacionDto dto)
    {
        var cap = await repository.GetByIdAsync(id);
        if (cap is null) return null;

        cap.Nombre = dto.Nombre;
        cap.Descripcion = dto.Descripcion;
        cap.DuracionHoras = dto.DuracionHoras;
        cap.FechaInicio = dto.FechaInicio;
        cap.FechaFin = dto.FechaFin;

        var updated = await repository.UpdateAsync(cap);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cap = await repository.GetByIdAsync(id);
        if (cap is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    private static CapacitacionDto MapToDto(Capacitacion c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        DuracionHoras = c.DuracionHoras,
        FechaInicio = c.FechaInicio,
        FechaFin = c.FechaFin,
        TotalInscritos = c.Inscripciones?.Count ?? 0
    };
}
