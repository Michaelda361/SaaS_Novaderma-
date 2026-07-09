using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
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
        if (colaborador is null) return null;
        var dto = MapToDto(colaborador);
        dto.CamposAdicionales = await repository.GetValoresPorColaboradorAsync(id);
        return dto;
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
            FechaIngreso = dto.FechaIngreso,
            Cedula = dto.Cedula,
            FechaExpedicion = dto.FechaExpedicion,
            FechaNacimiento = dto.FechaNacimiento,
            LugarNacimiento = dto.LugarNacimiento,
            TipoContrato = dto.TipoContrato,
            FechaIngresoContrato = dto.FechaIngresoContrato,
            SueldoBasico = dto.SueldoBasico,
            SubTransporte = dto.SubTransporte,
            AuxMediosTransporte = dto.AuxMediosTransporte,
            AuxTransporte = dto.AuxTransporte,
            ComisionVentas = dto.ComisionVentas,
            ComisionCobros = dto.ComisionCobros,
            Genero = ParseGenero(dto.Genero),
            AreaId = dto.AreaId,
            CargoId = dto.CargoId
        };

        var created = await repository.CreateAsync(colaborador);
        if (dto.CamposAdicionales is not null)
            await repository.SetValoresAdicionalesAsync(created.Id, dto.CamposAdicionales);

        var result = await repository.GetByIdAsync(created.Id);
        return result is null ? MapToDto(created) : MapToDto(result);
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
        colaborador.Cedula = dto.Cedula;
        colaborador.FechaExpedicion = dto.FechaExpedicion;
        colaborador.FechaNacimiento = dto.FechaNacimiento;
        colaborador.LugarNacimiento = dto.LugarNacimiento;
        colaborador.TipoContrato = dto.TipoContrato;
        colaborador.FechaIngresoContrato = dto.FechaIngresoContrato;
        colaborador.SueldoBasico = dto.SueldoBasico;
        colaborador.SubTransporte = dto.SubTransporte;
        colaborador.AuxMediosTransporte = dto.AuxMediosTransporte;
        colaborador.AuxTransporte = dto.AuxTransporte;
        colaborador.ComisionVentas = dto.ComisionVentas;
        colaborador.ComisionCobros = dto.ComisionCobros;
        colaborador.Genero = ParseGenero(dto.Genero);
        if (colaborador.AreaId != dto.AreaId)
        {
            colaborador.AreaId = dto.AreaId;
            colaborador.Area = null!;
        }
        if (colaborador.CargoId != dto.CargoId)
        {
            colaborador.CargoId = dto.CargoId;
            colaborador.Cargo = null!;
        }

        var updated = await repository.UpdateAsync(colaborador);
        if (dto.CamposAdicionales is not null)
            await repository.SetValoresAdicionalesAsync(id, dto.CamposAdicionales);

        var result = await repository.GetByIdAsync(id);
        return result is null ? MapToDto(updated) : MapToDto(result);
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

    private static GeneroColaborador ParseGenero(string genero)
        => Enum.TryParse<GeneroColaborador>(genero, ignoreCase: true, out var parsed)
            ? parsed
            : GeneroColaborador.NoInformado;

    private static ColaboradorDto MapToDto(Colaborador c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Apellido = c.Apellido,
        Email = c.Email,
        FechaIngreso = c.FechaIngreso,
        Cedula = c.Cedula,
        FechaExpedicion = c.FechaExpedicion,
        FechaNacimiento = c.FechaNacimiento,
        LugarNacimiento = c.LugarNacimiento,
        TipoContrato = c.TipoContrato,
        FechaIngresoContrato = c.FechaIngresoContrato,
        SueldoBasico = c.SueldoBasico,
        SubTransporte = c.SubTransporte,
        AuxMediosTransporte = c.AuxMediosTransporte,
        AuxTransporte = c.AuxTransporte,
        ComisionVentas = c.ComisionVentas,
        ComisionCobros = c.ComisionCobros,
        FechaSalida = c.FechaSalida,
        AreaNombre = c.Area?.Nombre ?? string.Empty,
        AreaId = c.AreaId,
        CargoNombre = c.Cargo?.Nombre ?? string.Empty,
        CargoId = c.CargoId,
        Rol = c.Rol.ToString(),
        Genero = c.Genero.ToString(),
        CamposAdicionales = new Dictionary<string, string?>()
    };
}
