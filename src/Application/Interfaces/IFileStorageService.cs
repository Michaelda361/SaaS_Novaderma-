namespace TalentManagement.Application.Interfaces;

/// <summary>
/// Abstracción de almacenamiento de archivos binarios.
/// Producción: Azure Blob Storage. Desarrollo: sistema de archivos local.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Sube un archivo. Devuelve la blobKey para recuperarlo. Máximo 10 MB.</summary>
    Task<string> UploadAsync(Stream contenido, string nombreArchivo, string contenedor, string mimeType);

    /// <summary>Descarga el archivo como bytes. Null si no existe.</summary>
    Task<byte[]?> DownloadAsync(string blobKey);

    /// <summary>Elimina el archivo. No lanza si no existe.</summary>
    Task DeleteAsync(string blobKey);

    /// <summary>URL firmada con expiración (default 1 hora) para descarga directa.</summary>
    Task<string> GetSignedUrlAsync(string blobKey, TimeSpan? expiry = null);
}
