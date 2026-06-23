using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TalentManagement.Infrastructure.Services;

public class LibreOfficeConverterService(ILogger<LibreOfficeConverterService> logger)
{
    private static readonly string[] WindowsPaths = [
        @"C:\Program Files\LibreOffice\program\soffice.exe",
        @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
    ];
    private static readonly string[] LinuxPaths = [
        "/usr/bin/soffice",
        "/usr/lib/libreoffice/program/soffice",
        "/opt/libreoffice/program/soffice",
    ];
    private static readonly Dictionary<string, string> ExtensionPorMime = new()
    {
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]   = "docx",
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = "pptx",
    };

    private static readonly Dictionary<string, string> ConvertFilterPorExtension = new()
    {
        ["docx"] = "pdf:writer_pdf_Export",
        ["pptx"] = "pdf:impress_pdf_Export",
    };

    public byte[]? ConvertirAPdf(byte[] archivoBytes, string mimeType)
    {
        if (!ExtensionPorMime.TryGetValue(mimeType, out var extension)) extension = "docx";
        return ConvertirAPdfInterno(archivoBytes, extension);
    }
    public byte[]? ConvertirDocxAPdf(byte[] docxBytes) => ConvertirAPdfInterno(docxBytes, "docx");
    private byte[]? ConvertirAPdfInterno(byte[] archivoBytes, string extension)
    {
        var soffice = EncontrarSoffice();
        if (soffice is null) { logger.LogWarning("LibreOffice no encontrado para {Ext}", extension); return null; }
        var tempDir = Path.Combine(Path.GetTempPath(), $"novahub_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var inputPath = Path.Combine(tempDir, $"documento.{extension}");
            File.WriteAllBytes(inputPath, archivoBytes);
            var filter = ConvertFilterPorExtension.TryGetValue(extension, out var filterName)
                ? filterName
                : "pdf";
            var psi = new ProcessStartInfo
            {
                FileName = soffice,
                Arguments = $"--headless --norestore --convert-to {filter} \"{inputPath}\" --outdir \"{tempDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi) ?? throw new InvalidOperationException("No se pudo iniciar LibreOffice.");
            var completado = process.WaitForExit(30_000);
            if (!completado) { process.Kill(entireProcessTree: true); logger.LogError("LibreOffice timeout"); return null; }
            if (process.ExitCode != 0) { logger.LogError("LibreOffice error {Code}", process.ExitCode); return null; }
            var outputPath = Path.Combine(tempDir, "documento.pdf");
            if (!File.Exists(outputPath)) { logger.LogError("PDF no generado en {Path}", outputPath); return null; }
            return File.ReadAllBytes(outputPath);
        }
        catch (Exception ex) { logger.LogError(ex, "Error convirtiendo {Ext}", extension); return null; }
        finally { try { Directory.Delete(tempDir, recursive: true); } catch { } }
    }

    public byte[]? ConvertirPptxAPng(byte[] pptxBytes)
    {
        var soffice = EncontrarSoffice();
        if (soffice is null) { logger.LogWarning("LibreOffice no encontrado para conversión de PNG"); return null; }
        var tempDir = Path.Combine(Path.GetTempPath(), $"novahub_png_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var inputPath = Path.Combine(tempDir, "documento.pptx");
            File.WriteAllBytes(inputPath, pptxBytes);
            var psi = new ProcessStartInfo
            {
                FileName = soffice,
                Arguments = $"--headless --norestore --convert-to png \"{inputPath}\" --outdir \"{tempDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi) ?? throw new InvalidOperationException("No se pudo iniciar LibreOffice.");
            var completado = process.WaitForExit(30_000);
            if (!completado) { process.Kill(entireProcessTree: true); logger.LogError("LibreOffice timeout"); return null; }
            var pngFile = Directory.GetFiles(tempDir, "*.png").FirstOrDefault();
            if (pngFile is null || !File.Exists(pngFile)) 
            { 
                logger.LogError("PNG no generado para la plantilla PPTX"); 
                return null; 
            }
            return File.ReadAllBytes(pngFile);
        }
        catch (Exception ex) { logger.LogError(ex, "Error convirtiendo PPTX a PNG"); return null; }
        finally { try { Directory.Delete(tempDir, recursive: true); } catch { } }
    }

    public bool EstaDisponible() => EncontrarSoffice() is not null;
    private static string? EncontrarSoffice() { var paths = OperatingSystem.IsWindows() ? WindowsPaths : LinuxPaths; return paths.FirstOrDefault(File.Exists); }
}