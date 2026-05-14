using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Certificados;

namespace TalentManagement.Application.Services;

public class CertificadoService(
    ICertificadoRepository repository,
    IFileStorageService storage)
{
    private const string ContenedorCert = "certificados";
    public async Task<List<CertificadoDto>> GetAllAsync()
    {
        var certs = await repository.GetAllAsync();
        return certs.Select(MapToDto).ToList();
    }

    public async Task<List<CertificadoDto>> GetByColaboradorAsync(int colaboradorId)
    {
        var certs = await repository.GetByColaboradorAsync(colaboradorId);
        return certs.Select(MapToDto).ToList();
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

    public async Task<CertificadoDto?> UpdateAsync(int id, CreateCertificadoDto dto)
    {
        var cert = await repository.GetByIdAsync(id);
        if (cert is null) return null;
        cert.Nombre = dto.Nombre;
        cert.Institucion = dto.Institucion;
        cert.FechaEmision = dto.FechaEmision;
        cert.FechaVencimiento = dto.FechaVencimiento;
        cert.UrlDocumento = dto.UrlDocumento;
        var updated = await repository.UpdateAsync(cert);
        return MapToDto(updated);
    }

    public async Task<List<CertificadoDto>> GetVencidosAsync()
    {
        var certs = await repository.GetVencidosAsync();
        return certs.Select(MapToDto).ToList();
    }

    public async Task<List<CertificadoDto>> GetProximosAVencerAsync(int dias = 30)
    {
        var certs = await repository.GetProximosAVencerAsync(dias);
        return certs.Select(MapToDto).ToList();
    }


    public async Task<byte[]?> GetPdfAsync(int id)
    {
        var cert = await repository.GetByIdAsync(id);
        if (cert is null) return null;

        // 1. Storage (nuevo flujo)
        if (!string.IsNullOrWhiteSpace(cert.PdfFileKey))
        {
            var bytes = await storage.DownloadAsync(cert.PdfFileKey);
            if (bytes is { Length: > 0 }) return bytes;
        }

        // 2. Fallback: binario legacy en SQL
        return cert.PdfBytes;
    }

    public async Task<Certificado?> GetCertificadoEntityAsync(int id) =>
        await repository.GetByIdAsync(id);

    public async Task<bool> ActualizarPdfAsync(int id, byte[] pdf)
    {
        var cert = await repository.GetByIdAsync(id);
        if (cert is null) return false;

        // Eliminar archivo anterior del storage si existe
        if (!string.IsNullOrWhiteSpace(cert.PdfFileKey))
            await storage.DeleteAsync(cert.PdfFileKey);

        // Subir nuevo PDF al storage
        var nombrePdf = $"certificado_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        using var ms = new MemoryStream(pdf);
        cert.PdfFileKey = await storage.UploadAsync(ms, nombrePdf, ContenedorCert, "application/pdf");

        // Limpiar legacy si existía
        cert.PdfBytes = null;

        await repository.UpdateAsync(cert);
        return true;
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
            : $"{c.Colaborador.Nombre} {c.Colaborador.Apellido}",
        CapacitacionId = c.CapacitacionId,
        CapacitacionNombre = c.Capacitacion?.Nombre,
        TienePdf = c.PdfFileKey is not null || c.PdfBytes is { Length: > 0 }
    };
}
