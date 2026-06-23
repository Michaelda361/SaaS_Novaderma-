using Microsoft.Extensions.Configuration;
using TalentManagement.Application.Interfaces;

namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Implementación mock de IFileStorageService para desarrollo local.
/// Guarda archivos en wwwroot/uploads/blobs en lugar de Azure Blob Storage.
/// </summary>
public class MockFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    private static readonly HashSet<string> MimeTypesPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/octet-stream",
    };

    public MockFileStorageService(IConfiguration config)
    {
        var contentRoot = config["ContentRootPath"] ?? AppContext.BaseDirectory;
        _basePath = Path.Combine(contentRoot, "wwwroot", "uploads", "blobs");
        Directory.CreateDirectory(_basePath);
        var baseAddress = config["BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5194";
        _baseUrl = $"{baseAddress}/uploads/blobs";
    }

    public async Task<string> UploadAsync(
        Stream contenido, string nombreArchivo, string contenedor, string mimeType)
    {
        if (!MimeTypesPermitidos.Contains(mimeType))
            throw new InvalidOperationException($"Tipo de archivo no permitido: {mimeType}");

        if (contenido.CanSeek && contenido.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("El archivo supera el límite de 10 MB.");

        var nombreSeguro = Path.GetFileName(nombreArchivo)
            .Replace("..", string.Empty)
            .Replace("/", string.Empty)
            .Replace("\\", string.Empty);

        var carpeta = Path.Combine(_basePath, contenedor);
        Directory.CreateDirectory(carpeta);

        var blobName = $"{Guid.NewGuid():N}_{nombreSeguro}";
        var rutaCompleta = Path.Combine(carpeta, blobName);

        await using var fs = File.Create(rutaCompleta);
        await contenido.CopyToAsync(fs);

        return $"{contenedor}/{blobName}";
    }

    public async Task<byte[]?> DownloadAsync(string blobKey)
    {
        var ruta = ResolverRuta(blobKey);
        if (!File.Exists(ruta)) return null;
        return await File.ReadAllBytesAsync(ruta);
    }

    public Task DeleteAsync(string blobKey)
    {
        var ruta = ResolverRuta(blobKey);
        if (File.Exists(ruta)) File.Delete(ruta);
        return Task.CompletedTask;
    }

    public Task<string> GetSignedUrlAsync(string blobKey, TimeSpan? expiry = null)
    {
        // En dev no hay firma — devuelve URL directa local
        var relativa = blobKey.Replace("\\", "/");
        return Task.FromResult($"{_baseUrl}/{relativa}");
    }

    private string ResolverRuta(string blobKey)
    {
        var relativa = blobKey.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(_basePath, relativa);
    }
}
