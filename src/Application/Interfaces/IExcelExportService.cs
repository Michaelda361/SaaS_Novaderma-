using System.Collections.Generic;
using TalentManagement.Shared.DTOs.ControlDocumental;

namespace TalentManagement.Application.Interfaces;

public interface IExcelExportService
{
    byte[] ExportListadoMaestro(ListadoMaestroDto listado, List<DocumentoControlDto> documentos);
}
