using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Capacitaciones;

namespace TalentManagement.Application.Services;

public class CapacitacionService(
    ICapacitacionRepository repository,
    IInscripcionRepository inscripcionRepository)
{
    public async Task<List<CapacitacionDto>> GetAllAsync()
    {
        var capacitaciones = await repository.GetAllAsync();
        return capacitaciones.Select(MapToDto).ToList();
    }

    public async Task<CapacitacionDto?> GetByIdAsync(int id)
    {
        var cap = await repository.GetByIdAsync(id);
        return cap is null ? null : MapToDto(cap);
    }

    public async Task<List<CapacitacionDto>> GetByAreaAsync(int areaId)
    {
        var items = await repository.GetByAreaAsync(areaId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<List<CapacitacionDto>> GetByColaboradorAsync(int colaboradorId)
    {
        var items = await repository.GetByColaboradorAsync(colaboradorId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<CapacitacionDto> CreateAsync(CreateCapacitacionDto dto)
    {
        var capacitacion = new Capacitacion
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            DuracionHoras = dto.DuracionHoras,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            AreaId = dto.AreaId,
            ColaboradorId = dto.ColaboradorId
        };

        var created = await repository.CreateAsync(capacitacion);

        // Si se asignó a un colaborador específico, inscribirlo automáticamente
        if (dto.ColaboradorId.HasValue)
        {
            var inscripcion = new Inscripcion
            {
                ColaboradorId = dto.ColaboradorId.Value,
                CapacitacionId = created.Id,
                FechaInscripcion = dto.FechaInicio
            };
            await inscripcionRepository.CreateAsync(inscripcion);
        }

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
        cap.AreaId = dto.AreaId;
        cap.ColaboradorId = dto.ColaboradorId;

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
        TotalInscritos = 0,
        AreaId = c.AreaId,
        AreaNombre = c.Area?.Nombre,
        ColaboradorId = c.ColaboradorId,
        ColaboradorNombre = c.Colaborador is null ? null : $"{c.Colaborador.Nombre} {c.Colaborador.Apellido}"
    };
}
