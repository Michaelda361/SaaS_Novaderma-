using System.IO;
using TalentManagement.Shared.DTOs.ControlDocumental;

namespace TalentManagement.Application.Interfaces;

public interface IExcelImportService
{
    CreateListadoMaestroDto ParseListadoMaestro(Stream stream, string fileName);
}
