using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TalentManagement.Infrastructure.Services;

/// <summary>
/// Convierte DOCX a PDF usando LibreOffice headless.
/// Fidelidad máxima — preserva fuentes, tablas, imágenes, márgenes y estilos originales.
/// </summary>
public class LibreOfficeConverterService(ILogger<LibreOfficeConverterService> logger)
{
    private static readonly string[] WindowsPaths =
    [
        @"C:\Program Files\LibreOffice\program\soffice.exe",
        @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
    ];

    private static readonly string[] LinuxPaths =
    [
        "/usr/bin/soffice",
        "/usr/lib/libreoffice/program/soffice",
        "/opt/libreoffice/program/soffice",
    ];

    /// <summary>
    /// Convierte bytes de un .docx a PDF.
    /// Devuelve null si LibreOffice no está instalado o falla la conversión.
    /// </summary>
    public byte[]? ConvertirDocxAPdf(byte[] docxBytes)
    {
        var soffice = EncontrarSoffice();
        if (soffice is null)
        {
            logger.LogWarning("LibreOffice no encontrado. Instálalo para habilitar la conversión DOCX→PDF fiel.");
            return null;
        }

        // Directorio temporal único por conversión para evitar colisiones concurrentes
        var tempDir = Path.Combine(Path.GetTempPath(), $"novahub_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, "documento.docx");
            File.WriteAllBytes(inputPath, docxBytes);

            var psi = new ProcessStartInfo
            {
                FileName = soffice,
                Arguments = $"--headless --norestore --convert-to pdf \"{inputPath}\" --outdir \"{tempDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("No se pudo iniciar LibreOffice.");

            var completado = process.WaitForExit(30_000);

            if (!completado)
            {
                process.Kill(entireProcessTree: true);
                logger.LogError("LibreOffice tardó más de 30s — proceso terminado.");
                return null;
            }

            if (process.ExitCode != 0)
            {
                var stderr = process.StandardError.ReadToEnd();
                logger.LogError("LibreOffice salió con código {Code}: {Error}", process.ExitCode, stderr);
                return null;
            }

            var outputPath = Path.Combine(tempDir, "documento.pdf");
            if (!File.Exists(outputPath))
            {
                logger.LogError("LibreOffice no generó el PDF en {Path}", outputPath);
                return null;
            }

            return File.ReadAllBytes(outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al convertir DOCX a PDF con LibreOffice.");
            return null;
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* ignorar */ }
        }
    }

    public bool EstaDisponible() => EncontrarSoffice() is not null;

    private static string? EncontrarSoffice()
    {
        var paths = OperatingSystem.IsWindows() ? WindowsPaths : LinuxPaths;
        return paths.FirstOrDefault(File.Exists);
    }
}
