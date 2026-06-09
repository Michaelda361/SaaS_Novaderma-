using System;
using System.Linq;
using ClosedXML.Excel;
using System.IO;

class Program
{
    static void Main()
    {
        var path = @"C:\Users\Laboratorios\Desktop\Libro1 prueba importacion.xlsx";
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var wb = new XLWorkbook(fs);

        Console.WriteLine($"Sheets: {string.Join(", ", wb.Worksheets.Select(w => $"'{w.Name}'"))}");
        Console.WriteLine();

        foreach (var ws in wb.Worksheets)
        {
            Console.WriteLine($"=== Sheet: '{ws.Name}' ===");
            var usedRows = ws.RowsUsed().ToList();
            Console.WriteLine($"RowsUsed(): {usedRows.Count}");
            Console.WriteLine();

            // Show ONLY first 3 rows (header + 2 data)
            foreach (var row in usedRows.Take(3))
            {
                var cells = row.CellsUsed().ToList();
                Console.WriteLine($"Row {row.RowNumber()} ({cells.Count} cells):");
                foreach (var c in cells)
                    Console.WriteLine($"  [{c.Address.ColumnLetter}] = '{c.GetString()?.Trim()}'");
            }
            Console.WriteLine();

            // Count data records
            int dataCount = usedRows.Skip(1).Count(row =>
                row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(
                    c.GetString()?.Trim()?.Trim('\u00A0', '\u200B', '\uFEFF'))));
            Console.WriteLine($"Data records (rows 2+): {dataCount}");

            // Check if Codigo and Nombre columns exist
            if (usedRows.Any())
            {
                var headerRow = usedRows.First();
                var headers = headerRow.CellsUsed()
                    .Select(c => c.GetString()?.Trim().ToLowerInvariant()
                        .Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u")
                        .Replace(" ", ""))
                    .ToList();
                bool hasCodigo = headers.Any(h => h is "codigo" or "codigodedocumento" or "cod");
                bool hasNombre = headers.Any(h => h is "nombre" or "nombrededocumento" or "titulo" or "name");
                Console.WriteLine($"\nHas 'Codigo' column: {hasCodigo}");
                Console.WriteLine($"Has 'Nombre' column: {hasNombre}");
                Console.WriteLine($"Will import correctly: {hasCodigo && hasNombre}");
            }
        }
    }
}
