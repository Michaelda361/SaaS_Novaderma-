using TalentManagement.Domain.Enums;

namespace TalentManagement.Application.Interfaces;

public interface ISharePointService
{
    Task<(string itemId, string url)> SubirArchivoOficialAsync(
        Stream contenido, string nombreArchivo, TipoDocumento tipo);

    Task<(string itemId, string url)> SubirArchivoPropuestaAsync(
        Stream contenido, string nombreArchivo, int documentoId);

    Task MoverArchivoPropuestaAOficialAsync(
        string itemIdPropuesta, string nombreArchivo, TipoDocumento tipo);

    Task<string> ObtenerUrlDescargaAsync(string itemId);

    Task<string> ObtenerUrlEdicionAsync(string itemId, string nombreArchivo);

    Task EliminarArchivoAsync(string itemId);
}
