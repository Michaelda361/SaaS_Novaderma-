using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TalentManagement.Application.Services;
using TalentManagement.Server.Hubs;
using TalentManagement.Server.Services;
using TalentManagement.Shared.DTOs.Inscripciones;

namespace TalentManagement.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class InscripcionesController(
    InscripcionService service,
    CurrentUserService currentUser,
    IHubContext<NotificacionesHub> hub,
    ILogger<InscripcionesController> logger) : ControllerBase
{
    [HttpGet("capacitacion/{capacitacionId:int}")]
    public async Task<IActionResult> GetByCapacitacion(int capacitacionId)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
            return StatusCode(403, new { message = "No tienes permiso para ver los inscritos." });
        return Ok(await service.GetByCapacitacionAsync(capacitacionId));
    }

    [HttpGet("capacitacion/{capacitacionId:int}/historial")]
    public async Task<IActionResult> GetByCapacitacionHistorial(int capacitacionId)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
            return StatusCode(403, new { message = "No tienes permiso." });
        return Ok(await service.GetByCapacitacionHistorialAsync(capacitacionId));
    }

    [HttpGet("colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> GetByColaborador(int colaboradorId)
    {
        // Jefe/Admin pueden ver inscripciones de cualquier colaborador
        if (await currentUser.PuedeGestionarPlantillasAsync())
            return Ok(await service.GetByColaboradorAsync(colaboradorId));

        // Colaborador solo puede ver sus propias inscripciones
        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Forbid();
        if (miId != colaboradorId) return Forbid();

        return Ok(await service.GetByColaboradorAsync(colaboradorId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInscripcionDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var (result, error) = await service.CreateAsync(dto);
        if (error is not null) return Conflict(new { message = error });

        // Notificar al colaborador si está conectado al hub
        var connId = NotificacionesHub.GetConnectionId(result!.ColaboradorId);
        logger.LogInformation("[Inscripciones] ColaboradorId={Id} ConnId={ConnId}",
            result.ColaboradorId, connId ?? "(no conectado)");

        if (connId is not null)
        {
            try
            {
                await hub.Clients.Client(connId).SendAsync("InscripcionCreada", result);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al enviar notificación de inscripción creada al colaborador {ColId}.", result.ColaboradorId);
            }
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInscripcionDto dto)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var result = await service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync()) return Forbid();
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/recursos/{recursoId:int}/visto")]
    public async Task<IActionResult> MarcarRecursoVisto(int id, int recursoId)
    {
        var miId = await currentUser.GetColaboradorIdAsync();
        var inscripcion = await service.GetByIdAsync(id);
        if (inscripcion is null) return NotFound();

        // Solo el colaborador de la inscripción o un gestor/admin pueden marcar como visto
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            if (miId is null || miId != inscripcion.ColaboradorId)
                return Forbid();
        }

        var result = await service.MarcarRecursoVistoAsync(id, recursoId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Devuelve el historial completo (inscripciones + resultados) en 3 queries planas.
    /// Reemplaza el N+1 masivo de CargarHistorial en Capacitaciones.razor.
    /// </summary>
    [HttpGet("historial-completo")]
    public async Task<IActionResult> GetHistorialCompleto()
    {
        if (await currentUser.PuedeGestionarPlantillasAsync())
            return Ok(await service.GetHistorialCompletoAsync());

        var miId = await currentUser.GetColaboradorIdAsync();
        if (miId is null) return Forbid();

        return Ok(await service.GetHistorialCompletoAsync(miId.Value));
    }

    [HttpGet("exportar-todo")]
    public async Task<IActionResult> ExportarTodoExcel()
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
            return StatusCode(403, new { message = "No tienes permiso." });

        var filas = await service.GetHistorialCompletoAsync();
        var filasCompletadas = filas.Where(f => f.Inscripcion.FechaFinalizacion.HasValue).ToList();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.AddWorksheet("Historial Consolidado");

        sheet.Cell(1, 1).Value = "Historial Consolidado de Capacitaciones";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(1, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#1e293b");

        sheet.Cell(2, 1).Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
        sheet.Cell(2, 1).Style.Font.Italic = true;
        sheet.Cell(2, 1).Style.Font.FontSize = 10;
        sheet.Cell(2, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#64748b");

        string[] headers = [
            "Colaborador", "Email", "Área", "Cargo", "Capacitación", 
            "Fecha Inscripción", "Fecha Finalización", "Calificación", "Estado", "Cuestionario"
        ];

        sheet.Row(4).Height = 26;
        for (int col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(4, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0d6efd");
            cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromHtml("#cbd5e1");
        }

        int row = 5;
        foreach (var h in filasCompletadas)
        {
            sheet.Row(row).Height = 20;

            sheet.Cell(row, 1).Value = h.Inscripcion.ColaboradorNombre;
            sheet.Cell(row, 2).Value = h.Inscripcion.ColaboradorEmail;
            sheet.Cell(row, 3).Value = h.Inscripcion.ColaboradorArea;
            sheet.Cell(row, 4).Value = h.Inscripcion.ColaboradorCargo;
            sheet.Cell(row, 5).Value = h.Inscripcion.CapacitacionNombre;
            sheet.Cell(row, 6).Value = h.Inscripcion.FechaInscripcion.ToString("dd/MM/yyyy");
            sheet.Cell(row, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            sheet.Cell(row, 7).Value = h.Inscripcion.FechaFinalizacion?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—";
            sheet.Cell(row, 7).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            if (h.Inscripcion.Calificacion.HasValue)
            {
                var val = h.Inscripcion.Calificacion.Value;
                var cellCal = sheet.Cell(row, 8);
                cellCal.Value = val;
                cellCal.Style.NumberFormat.Format = "0.##";
                cellCal.Style.Font.Bold = true;
                cellCal.Style.Font.FontColor = val >= 7 
                    ? ClosedXML.Excel.XLColor.FromHtml("#15803d") 
                    : ClosedXML.Excel.XLColor.FromHtml("#b91c1c");
                cellCal.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
            }
            else
            {
                sheet.Cell(row, 8).Value = "—";
                sheet.Cell(row, 8).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            var cellEstado = sheet.Cell(row, 9);
            cellEstado.Style.Font.Bold = true;
            cellEstado.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            if (h.Resultado is null)
            {
                cellEstado.Value = "Pendiente";
                cellEstado.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f1f5f9");
                cellEstado.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#475569");
            }
            else if (h.Resultado.Aprobado)
            {
                cellEstado.Value = "Aprobado";
                cellEstado.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#d1e7dd");
                cellEstado.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#0f5132");
            }
            else
            {
                cellEstado.Value = "No aprobado";
                cellEstado.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f8d7da");
                cellEstado.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#842029");
            }

            sheet.Cell(row, 10).Value = h.Resultado is not null ? $"{h.Resultado.Correctas}/{h.Resultado.TotalPreguntas}" : "—";
            sheet.Cell(row, 10).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            if (row % 2 != 0)
            {
                for (int col = 1; col <= headers.Length; col++)
                {
                    if (col != 9)
                    {
                        sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f8fafc");
                    }
                }
            }

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = sheet.Cell(row, col);
                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromHtml("#e2e8f0");
                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            }

            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new System.IO.MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "historial_completo_capacitaciones.xlsx");
    }

    [HttpGet("exportar/colaborador/{colaboradorId:int}")]
    public async Task<IActionResult> ExportarColaboradorExcel(int colaboradorId)
    {
        if (!await currentUser.PuedeGestionarPlantillasAsync())
        {
            var miId = await currentUser.GetColaboradorIdAsync();
            if (miId is null || miId != colaboradorId)
                return Forbid();
        }

        var filas = await service.GetHistorialCompletoAsync(colaboradorId);
        var filasCompletadas = filas.Where(f => f.Inscripcion.FechaFinalizacion.HasValue).ToList();

        if (!filasCompletadas.Any())
            return NotFound(new { message = "El colaborador no tiene historial de capacitaciones." });

        var primerItem = filasCompletadas.First().Inscripcion;
        var colaboradorNombre = primerItem.ColaboradorNombre;
        var colaboradorCargo = primerItem.ColaboradorCargo;
        var colaboradorArea = primerItem.ColaboradorArea;

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.AddWorksheet("Historial Colaborador");

        sheet.Cell(1, 1).Value = $"Historial de Capacitaciones: {colaboradorNombre}";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(1, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#1e293b");

        sheet.Cell(2, 1).Value = $"{colaboradorCargo} · {colaboradorArea}";
        sheet.Cell(2, 1).Style.Font.FontSize = 11;
        sheet.Cell(2, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#475569");

        sheet.Cell(3, 1).Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
        sheet.Cell(3, 1).Style.Font.Italic = true;
        sheet.Cell(3, 1).Style.Font.FontSize = 9;
        sheet.Cell(3, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#64748b");

        string[] headers = [
            "Capacitación", "Fecha Inscripción", "Fecha Finalización", "Calificación", "Estado", "Cuestionario"
        ];

        sheet.Row(5).Height = 26;
        for (int col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(5, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0d6efd");
            cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromHtml("#cbd5e1");
        }

        int row = 6;
        foreach (var h in filasCompletadas)
        {
            sheet.Row(row).Height = 20;

            sheet.Cell(row, 1).Value = h.Inscripcion.CapacitacionNombre;
            sheet.Cell(row, 2).Value = h.Inscripcion.FechaInscripcion.ToString("dd/MM/yyyy");
            sheet.Cell(row, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            sheet.Cell(row, 3).Value = h.Inscripcion.FechaFinalizacion?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—";
            sheet.Cell(row, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            if (h.Inscripcion.Calificacion.HasValue)
            {
                var val = h.Inscripcion.Calificacion.Value;
                var cellCal = sheet.Cell(row, 4);
                cellCal.Value = val;
                cellCal.Style.NumberFormat.Format = "0.##";
                cellCal.Style.Font.Bold = true;
                cellCal.Style.Font.FontColor = val >= 7 
                    ? ClosedXML.Excel.XLColor.FromHtml("#15803d") 
                    : ClosedXML.Excel.XLColor.FromHtml("#b91c1c");
                cellCal.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
            }
            else
            {
                sheet.Cell(row, 4).Value = "—";
                sheet.Cell(row, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            var cellEstado = sheet.Cell(row, 5);
            cellEstado.Style.Font.Bold = true;
            cellEstado.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            if (h.Resultado is null)
            {
                cellEstado.Value = "Pendiente";
                cellEstado.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f1f5f9");
                cellEstado.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#475569");
            }
            else if (h.Resultado.Aprobado)
            {
                cellEstado.Value = "Aprobado";
                cellEstado.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#d1e7dd");
                cellEstado.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#0f5132");
            }
            else
            {
                cellEstado.Value = "No aprobado";
                cellEstado.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f8d7da");
                cellEstado.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#842029");
            }

            sheet.Cell(row, 6).Value = h.Resultado is not null ? $"{h.Resultado.Correctas}/{h.Resultado.TotalPreguntas}" : "—";
            sheet.Cell(row, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            if (row % 2 != 0)
            {
                for (int col = 1; col <= headers.Length; col++)
                {
                    if (col != 5)
                    {
                        sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f8fafc");
                    }
                }
            }

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = sheet.Cell(row, col);
                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromHtml("#e2e8f0");
                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            }

            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new System.IO.MemoryStream();
        workbook.SaveAs(stream);
        var safeFileName = $"historial_{colaboradorNombre.Replace(" ", "_")}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", safeFileName);
    }
}
