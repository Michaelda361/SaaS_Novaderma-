using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using TalentManagement.Application.Interfaces;

namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Implementación de IFileStorageService sobre Azure Blob Storage.
/// Configuración requerida en appsettings.json:
/// "AzureStorage": {
///   "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
/// }
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _client;
    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> MimeTypesPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/octet-stream",
    };

    private readonly bool _disabled;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connStr = configuration["AzureStorage:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connStr) || connStr.StartsWith("REEMPLAZA"))
        {
            _disabled = true;
            _client = null!;
            return;
        }

        try
        {
            _client = new BlobServiceClient(connStr);
        }
        catch (Exception)
        {
            _disabled = true;
            _client = null!;
        }
    }

    public async Task<string> UploadAsync(
        Stream contenido, string nombreArchivo, string contenedor, string mimeType)
    {
        if (_disabled) return await Task.FromResult(string.Empty);

        if (!MimeTypesPermitidos.Contains(mimeType))
            throw new InvalidOperationException($"Tipo de archivo no permitido: {mimeType}");

        if (contenido.CanSeek && contenido.Length > MaxBytes)
            throw new InvalidOperationException("El archivo supera el límite de 10 MB.");

        // Prevenir path traversal
        var nombreSeguro = Path.GetFileName(nombreArchivo)
            .Replace("..", string.Empty)
            .Replace("/", string.Empty)
            .Replace("\\", string.Empty);

        var blobKey = $"{contenedor}/{Guid.NewGuid():N}_{nombreSeguro}";

        var containerClient = _client.GetBlobContainerClient(contenedor);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(blobKey);
        await blobClient.UploadAsync(contenido, new BlobHttpHeaders { ContentType = mimeType });

        return blobKey;
    }

    public async Task<byte[]?> DownloadAsync(string blobKey)
    {
        if (_disabled) return await Task.FromResult<byte[]?>(null);

        var (contenedor, nombre) = ParseKey(blobKey);
        var blobClient = _client.GetBlobContainerClient(contenedor).GetBlobClient(nombre);

        if (!await blobClient.ExistsAsync()) return null;

        using var ms = new MemoryStream();
        await blobClient.DownloadToAsync(ms);
        return ms.ToArray();
    }

    public async Task DeleteAsync(string blobKey)
    {
        if (_disabled) return;

        var (contenedor, nombre) = ParseKey(blobKey);
        var blobClient = _client.GetBlobContainerClient(contenedor).GetBlobClient(nombre);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<string> GetSignedUrlAsync(string blobKey, TimeSpan? expiry = null)
    {
        if (_disabled) return await Task.FromResult(string.Empty);

        var (contenedor, nombre) = ParseKey(blobKey);
        var blobClient = _client.GetBlobContainerClient(contenedor).GetBlobClient(nombre);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = contenedor,
            BlobName = nombre,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromHours(1)),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var uri = blobClient.GenerateSasUri(sasBuilder);
        return await Task.FromResult(uri.ToString());
    }

    private static (string contenedor, string nombre) ParseKey(string blobKey)
    {
        var idx = blobKey.IndexOf('/');
        if (idx < 0) throw new ArgumentException($"blobKey inválido: {blobKey}");
        return (blobKey[..idx], blobKey[(idx + 1)..]);
    }
}
