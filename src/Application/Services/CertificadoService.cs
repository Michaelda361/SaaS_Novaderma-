using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Certificados;

namespace TalentManagement.Application.Services;

public class CertificadoService(ICertificadoRepository repository)
{
    public async Task<IEnumerable<CertificadoDto>> GetByColaboradorAsync(int colaboradorId)
    {
        var certs = await repository.GetByColaboradorAsync(colaboradorId);
        return certs.Select(MapToDto);
    }

    public async Task<CertificadoDto?> GetByIdAsync(int id)
    {
        var cert = await repository.GetByIdAsync(id);
        return cert is null ? null : MapToDto(cert);
    }

    public async Task<CertificadoDto> CreateAsync(CreateCertificadoDto dto)
    {
        var certificado = new Certificado
        {
            Nombre = dto.Nombre,
            Institucion = dto.Institucion,
            FechaEmision = dto.FechaEmision,
            FechaVencimiento = dto.FechaVencimiento,
            UrlDocumento = dto.UrlDocumento,
            ColaboradorId = dto.ColaboradorId
        };

        var created = await repository.CreateAsync(certificado);
        return MapToDto(created);
    }

    public async Task<IEnumerable<CertificadoDto>> GetVencidosAsync()
    {
        var certs = await repository.GetVencidosAsync();
        return certs.Select(MapToDto);
    }

    public async Task<IEnumerable<CertificadoDto>> GetProximosAVencerAsync(int dias = 30)
    {
        var certs = await repository.GetProximosAVencerAsync(dias);
        return certs.Select(MapToDto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cert = await repository.GetByIdAsync(id);
        if (cert is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    private static CertificadoDto MapToDto(Certificado c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Institucion = c.Institucion,
        FechaEmision = c.FechaEmision,
        FechaVencimiento = c.FechaVencimiento,
        UrlDocumento = c.UrlDocumento,
        ColaboradorId = c.ColaboradorId,
        ColaboradorNombre = c.Colaborador is null
            ? string.Empty
            : $"{c.Colaborador.Nombre} {c.Colaborador.Apellido}"
    };
}
