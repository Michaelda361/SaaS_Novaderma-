using Microsoft.Extensions.Configuration;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Implementación mock de ISharePointService para desarrollo/testing local.
/// Guarda archivos en la carpeta configurada en lugar de SharePoint.
/// </summary>
public class MockSharePointService : ISharePointService
{
    private readonly string _uploadsPath;
    private readonly string _baseUrl;

    public MockSharePointService(IConfiguration config)
    {
        var contentRoot = config["ContentRootPath"]
            ?? AppContext.BaseDirectory;

        _uploadsPath = Path.Combine(contentRoot, "wwwroot", "uploads");
        Directory.CreateDirectory(_uploadsPath);

        var baseAddress = config["BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5194";
        _baseUrl = $"{baseAddress}/uploads";
    }

    public async Task<(string itemId, string url)> SubirArchivoOficialAsync(
        Stream contenido, string nombreArchivo, TipoDocumento tipo)
    {
        var carpeta = Path.Combine(_uploadsPath, tipo.ToString());
        Directory.CreateDirectory(carpeta);
        return await GuardarAsync(contenido, nombreArchivo, carpeta, tipo.ToString());
    }

    public async Task<(string itemId, string url)> SubirArchivoPropuestaAsync(
        Stream contenido, string nombreArchivo, int documentoId)
    {
        var carpeta = Path.Combine(_uploadsPath, "_propuestas", documentoId.ToString());
        Directory.CreateDirectory(carpeta);
        return await GuardarAsync(contenido, nombreArchivo, carpeta,
            $"_propuestas/{documentoId}");
    }

    public Task MoverArchivoPropuestaAOficialAsync(
        string itemIdPropuesta, string nombreArchivo, TipoDocumento tipo)
    {
        var destino = Path.Combine(_uploadsPath, tipo.ToString());
        Directory.CreateDirectory(destino);

        var origen = ResolverRuta(itemIdPropuesta);
        if (File.Exists(origen))
            File.Move(origen, Path.Combine(destino, nombreArchivo), overwrite: true);

        return Task.CompletedTask;
    }

    public Task<string> ObtenerUrlDescargaAsync(string itemId)
    {
        var relativa = itemId.Replace("\\", "/");
        return Task.FromResult($"{_baseUrl}/{relativa}");
    }

    public Task<string> ObtenerUrlEdicionAsync(string itemId, string nombreArchivo)
    {
        // Si el itemId ya es una URL de OneDrive/SharePoint, devolverla directamente
        if (itemId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(itemId);

        // Para archivos locales, devolver la URL de descarga directa
        // (Office Online no puede acceder a localhost)
        var urlArchivo = $"{_baseUrl}/{itemId.Replace("\\", "/")}";
        return Task.FromResult(urlArchivo);
    }

    public Task EliminarArchivoAsync(string itemId)
    {
        var ruta = ResolverRuta(itemId);
        if (File.Exists(ruta)) File.Delete(ruta);
        return Task.CompletedTask;
    }

    private async Task<(string itemId, string url)> GuardarAsync(
        Stream contenido, string nombreArchivo, string carpeta, string subRuta)
    {
        var nombre = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{nombreArchivo}";
        var rutaCompleta = Path.Combine(carpeta, nombre);

        await using var fs = File.Create(rutaCompleta);
        await contenido.CopyToAsync(fs);

        var itemId = $"{subRuta}/{nombre}";
        var url = $"{_baseUrl}/{subRuta}/{Uri.EscapeDataString(nombre)}";
        return (itemId, url);
    }

    private string ResolverRuta(string itemId)
    {
        var relativa = itemId.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(_uploadsPath, relativa);
    }
}
