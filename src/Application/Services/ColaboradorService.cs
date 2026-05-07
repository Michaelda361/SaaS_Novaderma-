using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Colaboradores;

namespace TalentManagement.Application.Services;

public class ColaboradorService(IColaboradorRepository repository)
{
    public async Task<List<ColaboradorDto>> GetAllAsync()
    {
        var colaboradores = await repository.GetAllAsync();
        return colaboradores.Select(MapToDto).ToList();
    }

    public async Task<ColaboradorDto?> GetByIdAsync(int id)
    {
        var colaborador = await repository.GetByIdAsync(id);
        return colaborador is null ? null : MapToDto(colaborador);
    }

    public async Task<ColaboradorDto> CreateAsync(CreateColaboradorDto dto)
    {
        // Verificar email único entre colaboradores activos
        var existente = await repository.GetByEmailAsync(dto.Email);
        if (existente is not null)
            throw new InvalidOperationException(
                $"Ya existe un colaborador registrado con el email '{dto.Email}'.");

        var colaborador = new Colaborador
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Email = dto.Email,
            Telefono = dto.Telefono,
            FechaIngreso = dto.FechaIngreso,
            Cedula = dto.Cedula,
            TipoContrato = dto.TipoContrato,
            SueldoBasico = dto.SueldoBasico,
            Ciudad = dto.Ciudad,
            AreaId = dto.AreaId,
            CargoId = dto.CargoId,
            SupervisorId = dto.SupervisorId
        };

        var created = await repository.CreateAsync(colaborador);
        return MapToDto(created);
    }

    public async Task<ColaboradorDto?> UpdateAsync(int id, UpdateColaboradorDto dto)
    {
        var colaborador = await repository.GetByIdAsync(id);
        if (colaborador is null) return null;

        // Verificar email único solo si cambió
        if (!string.Equals(colaborador.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existente = await repository.GetByEmailAsync(dto.Email);
            if (existente is not null && existente.Id != id)
                throw new InvalidOperationException(
                    $"Ya existe un colaborador registrado con el email '{dto.Email}'.");
        }

        colaborador.Nombre = dto.Nombre;
        colaborador.Apellido = dto.Apellido;
        colaborador.Email = dto.Email;
        colaborador.Telefono = dto.Telefono;
        colaborador.Cedula = dto.Cedula;
        colaborador.TipoContrato = dto.TipoContrato;
        colaborador.SueldoBasico = dto.SueldoBasico;
        colaborador.Ciudad = dto.Ciudad;
        colaborador.AreaId = dto.AreaId;
        colaborador.CargoId = dto.CargoId;
        colaborador.SupervisorId = dto.SupervisorId;

        var updated = await repository.UpdateAsync(colaborador);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var colaborador = await repository.GetByIdAsync(id);
        if (colaborador is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<List<ColaboradorDto>> GetInactivosAsync()
    {
        var inactivos = await repository.GetInactivosAsync();
        return inactivos.Select(MapToDto).ToList();
    }

    public async Task<bool> RestaurarAsync(int id)
    {
        await repository.RestaurarAsync(id);
        return true;
    }

    public async Task<ColaboradorDto?> CambiarRolAsync(int id, string rol)
    {
        var colaborador = await repository.GetByIdAsync(id);
        if (colaborador is null) return null;

        if (!Enum.TryParse<Domain.Enums.RolUsuario>(rol, ignoreCase: true, out var rolEnum))
            throw new ArgumentException($"Rol inválido: {rol}");

        colaborador.Rol = rolEnum;
        var updated = await repository.UpdateAsync(colaborador);
        return MapToDto(updated);
    }

    private static ColaboradorDto MapToDto(Colaborador c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Apellido = c.Apellido,
        Email = c.Email,
        Telefono = c.Telefono,
        FechaIngreso = c.FechaIngreso,
        Cedula = c.Cedula,
        TipoContrato = c.TipoContrato,
        SueldoBasico = c.SueldoBasico,
        Ciudad = c.Ciudad,
        AreaNombre = c.Area?.Nombre ?? string.Empty,
        AreaId = c.AreaId,
        CargoNombre = c.Cargo?.Nombre ?? string.Empty,
        CargoId = c.CargoId,
        SupervisorNombre = c.Supervisor is null ? null : $"{c.Supervisor.Nombre} {c.Supervisor.Apellido}",
        Rol = c.Rol.ToString(),
    };
}
