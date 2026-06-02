using System.IO;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
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

    public async Task<CertificadoDto> CreateAsync(Certificado certificado, byte[]? pdfBytes = null, string? generatedBy = null)
    {
        var created = await repository.CreateAsync(certificado);
        if (pdfBytes is null || pdfBytes.Length == 0)
            return MapToDto(created);

        await GuardarPdfEnStorageAsync(created, pdfBytes, generatedBy);
        var updated = await repository.GetByIdAsync(created.Id) ?? created;
        return MapToDto(updated);
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

    public async Task<bool> ActualizarPdfAsync(int id, byte[] pdf, string? generatedBy = null)
    {
        var cert = await repository.GetByIdAsync(id);
        if (cert is null) return false;

        await GuardarPdfEnStorageAsync(cert, pdf, generatedBy);
        return true;
    }

    public async Task<bool> MarkDownloadedAsync(int id, string? downloadedBy = null)
    {
        var cert = await repository.GetByIdAsync(id);
        if (cert is null) return false;

        cert.Status = CertificadoStatus.Downloaded;
        if (!string.IsNullOrWhiteSpace(downloadedBy)) cert.GeneratedBy = downloadedBy;
        await repository.UpdateAsync(cert);
        return true;
    }

    public async Task<bool> RegistrarEventoAsync(int id, string tipo, string descripcion)
    {
        var cert = await repository.GetByIdAsync(id);
        if (cert is null) return false;

        await repository.AddEventoAsync(new CertificadoEvento
        {
            CertificadoId = id,
            Tipo = tipo,
            Descripcion = descripcion,
            Fecha = DateTime.UtcNow
        });

        return true;
    }

    private async Task GuardarPdfEnStorageAsync(Certificado cert, byte[] pdf, string? generatedBy)
    {
        if (!string.IsNullOrWhiteSpace(cert.PdfFileKey))
            await storage.DeleteAsync(cert.PdfFileKey);

        var nombrePdf = $"certificado_{cert.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        using var ms = new MemoryStream(pdf);
        cert.PdfFileKey = await storage.UploadAsync(ms, nombrePdf, ContenedorCert, "application/pdf");
        cert.PdfBytes = null;
        cert.CertificateCode ??= $"C-{cert.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        cert.GeneratedAt = DateTime.UtcNow;
        cert.GeneratedBy = generatedBy;
        cert.Status = CertificadoStatus.Generated;
        await repository.UpdateAsync(cert);
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
        TipoArchivoCertificado = c.Capacitacion?.TipoArchivoCertificado,
        TienePdf = c.PdfFileKey is not null || c.PdfBytes is { Length: > 0 },
        CertificateCode = c.CertificateCode,
        GeneratedAt = c.GeneratedAt,
        GeneratedBy = c.GeneratedBy,
        Status = c.Status.ToString()
    };
}
