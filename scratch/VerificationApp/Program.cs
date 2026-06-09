using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TalentManagement.Infrastructure.Persistence;
using TalentManagement.Infrastructure.Repositories;
using TalentManagement.Application.Services;
using TalentManagement.Shared.DTOs.ControlDocumental;
using System.Collections.Generic;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("[TEST RUNNER] Starting Verification Client...");

        // Check Excel contents first
        CheckExcelContents();

        string baseUrl = "http://localhost:5194";
        string importUrl = $"{baseUrl}/api/v1/control-documental/listados-maestros/import";
        
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "asistemas@laboratoriosnovaderma.com");

        // 1. Verify health check / connection
        bool connected = false;
        for (int i = 0; i < 15; i++)
        {
            try
            {
                var response = await client.GetAsync($"{baseUrl}/api/v1/dev/usuario-activo");
                if (response.IsSuccessStatusCode)
                {
                    connected = true;
                    break;
                }
            }
            catch {}
            Console.WriteLine("[TEST RUNNER] Waiting for server to become active...");
            await Task.Delay(2000);
        }

        if (!connected)
        {
            Console.WriteLine("[TEST RUNNER] ERROR: Server did not start in time.");
            Environment.Exit(1);
        }

        Console.WriteLine("[TEST RUNNER] Server is healthy. Proceeding to import test...");

        // 2. Locate original excel
        string excelPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "prueba excel.xlsx");
        if (!File.Exists(excelPath))
        {
            excelPath = Path.Combine(Directory.GetCurrentDirectory(), "prueba excel.xlsx");
        }
        
        if (!File.Exists(excelPath))
        {
            Console.WriteLine($"[TEST RUNNER] ERROR: excel file not found at: {excelPath}");
            Environment.Exit(1);
        }

        Console.WriteLine($"[TEST RUNNER] Reading Excel file from: {excelPath}");
        byte[] excelBytes = await File.ReadAllBytesAsync(excelPath);

        // 3. Perform the first import via HTTP
        Console.WriteLine("[TEST RUNNER] Uploading/Importing Excel sheet (Import 1)...");
        var listadoId = await UploadExcelAsync(client, importUrl, excelBytes, "prueba excel.xlsx");
        Console.WriteLine($"[TEST RUNNER] Import 1 completed. Created Listado Maestro ID: {listadoId}");

        // 4. Perform export to verify we get data
        string exportUrl = $"{baseUrl}/api/v1/control-documental/listados-maestros/{listadoId}/export";
        Console.WriteLine($"[TEST RUNNER] Downloading exported Excel for ID {listadoId}...");
        var exportResponse = await client.GetAsync(exportUrl);
        if (!exportResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"[TEST RUNNER] ERROR: Failed to export. Status: {exportResponse.StatusCode}");
            Environment.Exit(1);
        }

        byte[] exportedBytes = await exportResponse.Content.ReadAsByteArrayAsync();
        string exportSavePath = Path.Combine(Directory.GetCurrentDirectory(), "exported_test.xlsx");
        await File.WriteAllBytesAsync(exportSavePath, exportedBytes);
        Console.WriteLine($"[TEST RUNNER] Exported file saved to: {exportSavePath} ({exportedBytes.Length} bytes)");

        // 5. Run Database-Level delta synchronizations to verify deactivation & reactivation of placeholder documents
        await RunDatabaseIntegrationTests(listadoId);

        Console.WriteLine("[TEST RUNNER] Verification client finished successfully!");
    }

    static async Task RunDatabaseIntegrationTests(int listadoId)
    {
        Console.WriteLine("\n[TEST RUNNER] --- Starting DB-Level Integration Tests ---");
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=TalentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;");
        
        using var context = new AppDbContext(optionsBuilder.Options);
        var repository = new ControlDocumentalRepository(context);
        var auditRepository = new AuditLogRepository(context);
        var colaboradorRepository = new ColaboradorRepository(context);
        
        var cache = new MemoryCache(new MemoryCacheOptions());
        var areaRepository = new AreaRepository(context, cache);
        
        var service = new ControlDocumentalService(repository, auditRepository, colaboradorRepository, areaRepository);

        // Fetch currently active documents for this listado
        var activeDocs = (await repository.GetDocumentosAsync(listadoId, null, null, null, null, null))
            .Where(d => d.Activo)
            .ToList();
        
        Console.WriteLine($"[TEST RUNNER] Currently active documents in database: {activeDocs.Count}");

        // Find BATCH RECORD PRODUCTO TERMINADO and another document (e.g. FT-SGI-030)
        var brDoc = activeDocs.FirstOrDefault(d => d.Nombre == "BATCH RECORD PRODUCTO TERMINADO");
        var otherDoc = activeDocs.FirstOrDefault(d => d.Codigo == "FT-SGI-030");

        if (brDoc == null || otherDoc == null)
        {
            Console.WriteLine("[TEST RUNNER] ERROR: Could not find test documents in active database.");
            Environment.Exit(1);
        }

        Console.WriteLine($"[TEST RUNNER] Test document 1: '{brDoc.Nombre}', Code='{brDoc.Codigo}', Activo={brDoc.Activo}");
        Console.WriteLine($"[TEST RUNNER] Test document 2: '{otherDoc.Nombre}', Code='{otherDoc.Codigo}', Activo={otherDoc.Activo}");

        // TEST 1: Synchronize with only FT-SGI-030 (omitting BATCH RECORD PRODUCTO TERMINADO)
        // Expected: BATCH RECORD PRODUCTO TERMINADO should be deactivated (Activo = false).
        Console.WriteLine("\n[TEST RUNNER] [TEST 1] Running synchronization omitting BATCH RECORD PRODUCTO TERMINADO...");

        var test1Docs = new List<TemplateDocumentoDto>
        {
            new TemplateDocumentoDto
            {
                Codigo = otherDoc.Codigo,
                Nombre = otherDoc.Nombre,
                ProcesoResponsable = otherDoc.ProcesoResponsable,
                Version = otherDoc.Version,
                Estado = otherDoc.Estado,
                FechaDocumento = otherDoc.FechaDocumento,
                OneDriveUrl = otherDoc.OneDriveUrl,
                ComentarioCambio = "TEST 1 Update"
            }
        };

        var updateDto = new CreateListadoMaestroDto
        {
            Nombre = "prueba excel",
            Descripcion = "Test",
            Documentos = test1Docs
        };

        await service.UpdateListadoAsync(listadoId, updateDto, "asistemas@laboratoriosnovaderma.com");

        // Verify status in DB (use a fresh DbContext context to clear tracking cache)
        using (var checkContext = new AppDbContext(optionsBuilder.Options))
        {
            var db = new ControlDocumentalRepository(checkContext);
            var dbBrDoc = (await db.GetDocumentosAsync(listadoId, null, null, null, null, null))
                .FirstOrDefault(d => d.Nombre == "BATCH RECORD PRODUCTO TERMINADO");
            var dbOtherDoc = (await db.GetDocumentosAsync(listadoId, null, null, null, null, null))
                .FirstOrDefault(d => d.Codigo == "FT-SGI-030");
            var totalActive = (await db.GetDocumentosAsync(listadoId, null, null, null, null, null))
                .Count(d => d.Activo);

            Console.WriteLine($"[TEST RUNNER] Results after TEST 1 sync:");
            Console.WriteLine($"[TEST RUNNER] Total Active Documents in DB: {totalActive}");
            Console.WriteLine($"[TEST RUNNER] BATCH RECORD PRODUCTO TERMINADO: Activo={dbBrDoc?.Activo ?? false}");
            Console.WriteLine($"[TEST RUNNER] FT-SGI-030: Activo={dbOtherDoc?.Activo ?? false}");

            if (dbBrDoc != null && dbBrDoc.Activo)
            {
                Console.WriteLine("[TEST RUNNER] ERROR: BATCH RECORD PRODUCTO TERMINADO was NOT deactivated!");
                Environment.Exit(1);
            }
            if (totalActive != 1)
            {
                Console.WriteLine($"[TEST RUNNER] ERROR: Expected exactly 1 active document, found: {totalActive}");
                Environment.Exit(1);
            }
            Console.WriteLine("[TEST RUNNER] [TEST 1 SUCCESS] Deactivation logic works perfectly!");
        }

        // TEST 2: Reactivate BATCH RECORD PRODUCTO TERMINADO by adding it back to the import
        // Expected: BATCH RECORD PRODUCTO TERMINADO should be reactivated (Activo = true) and total active docs should be 2.
        Console.WriteLine("\n[TEST RUNNER] [TEST 2] Running synchronization including both documents...");

        var test2Docs = new List<TemplateDocumentoDto>
        {
            new TemplateDocumentoDto
            {
                Codigo = otherDoc.Codigo,
                Nombre = otherDoc.Nombre,
                ProcesoResponsable = otherDoc.ProcesoResponsable,
                Version = otherDoc.Version,
                Estado = otherDoc.Estado,
                FechaDocumento = otherDoc.FechaDocumento,
                OneDriveUrl = otherDoc.OneDriveUrl,
                ComentarioCambio = "TEST 2 Update"
            },
            new TemplateDocumentoDto
            {
                Codigo = brDoc.Codigo,
                Nombre = brDoc.Nombre,
                ProcesoResponsable = brDoc.ProcesoResponsable,
                Version = brDoc.Version,
                Estado = brDoc.Estado,
                FechaDocumento = brDoc.FechaDocumento,
                OneDriveUrl = brDoc.OneDriveUrl,
                ComentarioCambio = "TEST 2 Reactivate"
            }
        };

        updateDto.Documentos = test2Docs;
        await service.UpdateListadoAsync(listadoId, updateDto, "asistemas@laboratoriosnovaderma.com");

        using (var checkContext = new AppDbContext(optionsBuilder.Options))
        {
            var db = new ControlDocumentalRepository(checkContext);
            var dbBrDoc = (await db.GetDocumentosAsync(listadoId, null, null, null, null, null))
                .FirstOrDefault(d => d.Nombre == "BATCH RECORD PRODUCTO TERMINADO");
            var dbOtherDoc = (await db.GetDocumentosAsync(listadoId, null, null, null, null, null))
                .FirstOrDefault(d => d.Codigo == "FT-SGI-030");
            var totalActive = (await db.GetDocumentosAsync(listadoId, null, null, null, null, null))
                .Count(d => d.Activo);

            Console.WriteLine($"[TEST RUNNER] Results after TEST 2 sync:");
            Console.WriteLine($"[TEST RUNNER] Total Active Documents in DB: {totalActive}");
            Console.WriteLine($"[TEST RUNNER] BATCH RECORD PRODUCTO TERMINADO: Activo={dbBrDoc?.Activo ?? false}");
            Console.WriteLine($"[TEST RUNNER] FT-SGI-030: Activo={dbOtherDoc?.Activo ?? false}");

            if (dbBrDoc == null || !dbBrDoc.Activo)
            {
                Console.WriteLine("[TEST RUNNER] ERROR: BATCH RECORD PRODUCTO TERMINADO was NOT reactivated!");
                Environment.Exit(1);
            }
            if (totalActive != 2)
            {
                Console.WriteLine($"[TEST RUNNER] ERROR: Expected exactly 2 active documents, found: {totalActive}");
                Environment.Exit(1);
            }
            Console.WriteLine("[TEST RUNNER] [TEST 2 SUCCESS] Reactivation logic works perfectly!");
        }

        Console.WriteLine("[TEST RUNNER] --- DB-Level Integration Tests Completed Successfully ---\n");
    }

    static void CheckExcelContents()
    {
        try
        {
            string excelPath = "prueba excel.xlsx";
            if (!File.Exists(excelPath))
            {
                excelPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "prueba excel.xlsx");
            }
            using var workbook = new XLWorkbook(excelPath);
            Console.WriteLine("[TEST RUNNER] Inspecting Excel contents via ClosedXML...");
            
            bool found = false;
            foreach (var ws in workbook.Worksheets)
            {
                foreach (var row in ws.RowsUsed())
                {
                    foreach (var c in row.CellsUsed())
                    {
                        string val = c.Value.ToString();
                        if (val.Contains("BATCH RECORD PRODUCTO TERMINADO"))
                        {
                            Console.WriteLine($"[TEST RUNNER] FOUND cell: Sheet='{ws.Name}', Address='{c.Address}', Value='{val}'");
                            found = true;
                        }
                    }
                }
            }
            if (!found)
            {
                Console.WriteLine("[TEST RUNNER] NOT FOUND anywhere in active cells of the Excel file.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TEST RUNNER] Error reading Excel check: {ex.Message}");
        }
    }

    static async Task<int> UploadExcelAsync(HttpClient client, string url, byte[] fileBytes, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", fileName);

        var response = await client.PostAsync(url, content);
        string responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[TEST RUNNER] ERROR on Import POST: {response.StatusCode}");
            Console.WriteLine($"[TEST RUNNER] Response: {responseString}");
            Environment.Exit(1);
        }

        int idStart = responseString.IndexOf("\"id\":");
        if (idStart == -1)
        {
            idStart = responseString.IndexOf("\"Id\":");
        }
        if (idStart == -1)
        {
            throw new Exception("Could not find listado ID in response JSON: " + responseString);
        }

        int valStart = idStart + 5;
        int valEnd = responseString.IndexOf(",", valStart);
        if (valEnd == -1)
        {
            valEnd = responseString.IndexOf("}", valStart);
        }

        string idStr = responseString.Substring(valStart, valEnd - valStart).Trim();
        return int.Parse(idStr);
    }
}
