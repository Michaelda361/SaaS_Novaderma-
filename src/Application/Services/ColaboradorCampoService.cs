using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Colaboradores;

namespace TalentManagement.Application.Services;

public class ColaboradorCampoService(IColaboradorRepository repository)
{
    public async Task<List<ColaboradorCampoDto>> GetAllAsync()
    {
        var campos = await repository.GetCamposDefinicionAsync();
        return campos.Select(MapToDto).ToList();
    }

    public async Task<ColaboradorCampoDto> CreateAsync(CreateColaboradorCampoDto dto)
    {
        var campo = new ColaboradorCampoDefinicion
        {
            CampoClave = string.IsNullOrWhiteSpace(dto.CampoClave) ? GenerateCampoClave(dto.Nombre) : dto.CampoClave!,
            Nombre = dto.Nombre,
            Tipo = Enum.TryParse<Domain.Enums.ColaboradorCampoTipo>(dto.Tipo, ignoreCase: true, out var tipo)
                ? tipo
                : Domain.Enums.ColaboradorCampoTipo.Texto,
            Requerido = dto.Requerido,
            Opciones = dto.Opciones,
            Orden = dto.Orden
        };

        var created = await repository.CreateCampoDefinicionAsync(campo);
        return MapToDto(created);
    }

    public async Task<ColaboradorCampoDto?> UpdateAsync(int id, UpdateColaboradorCampoDto dto)
    {
        var campo = await repository.GetCampoDefinicionAsync(id);
        if (campo is null) return null;

        campo.Nombre = dto.Nombre;
        campo.Tipo = Enum.TryParse<Domain.Enums.ColaboradorCampoTipo>(dto.Tipo, ignoreCase: true, out var tipo)
            ? tipo
            : Domain.Enums.ColaboradorCampoTipo.Texto;
        campo.Requerido = dto.Requerido;
        campo.Opciones = dto.Opciones;
        campo.Orden = dto.Orden;

        var updated = await repository.UpdateCampoDefinicionAsync(campo);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await repository.DeleteCampoDefinicionAsync(id);
        return true;
    }

    private static ColaboradorCampoDto MapToDto(ColaboradorCampoDefinicion campo) => new()
    {
        Id = campo.Id,
        CampoClave = campo.CampoClave,
        Nombre = campo.Nombre,
        Tipo = campo.Tipo.ToString(),
        Requerido = campo.Requerido,
        Opciones = campo.Opciones,
        Orden = campo.Orden
    };

    private static string GenerateCampoClave(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "campo" + Guid.NewGuid().ToString("N");
        var clave = new string(nombre.Trim()
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ')
            .Select(c => c == ' ' ? '_' : c)
            .ToArray());
        return string.IsNullOrWhiteSpace(clave) ? "campo" + Guid.NewGuid().ToString("N") : clave;
    }
}
