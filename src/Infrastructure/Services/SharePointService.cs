using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Infrastructure.Services;

public class SharePointService : ISharePointService
{
    private readonly GraphServiceClient _graph;
    private readonly string _siteId;
    private readonly string _biblioteca;

    public SharePointService(IConfiguration config)
    {
        var sp = config.GetSection("SharePoint");
        var credential = new ClientSecretCredential(
            sp["TenantId"], sp["ClientId"], sp["ClientSecret"]);

        _graph = new GraphServiceClient(credential,
            ["https://graph.microsoft.com/.default"]);

        _siteId = sp["SiteId"] ?? throw new InvalidOperationException(
            "SharePoint:SiteId no configurado");
        _biblioteca = sp["BibliotecaDocumentos"] ?? "Documentos";
    }

    public async Task<(string itemId, string url)> SubirArchivoOficialAsync(
        Stream contenido, string nombreArchivo, TipoDocumento tipo)
    {
        var ruta = $"{_biblioteca}/{tipo}/{nombreArchivo}";
        return await SubirAsync(contenido, ruta);
    }

    public async Task<(string itemId, string url)> SubirArchivoPropuestaAsync(
        Stream contenido, string nombreArchivo, int documentoId)
    {
        var ruta = $"{_biblioteca}/_propuestas-pendientes/{documentoId}/{nombreArchivo}";
        return await SubirAsync(contenido, ruta);
    }

    public async Task MoverArchivoPropuestaAOficialAsync(
        string itemIdPropuesta, string nombreArchivo, TipoDocumento tipo)
    {
        try
        {
            var driveId = await GetDriveIdAsync();
            var destino = new DriveItem
            {
                Name = nombreArchivo,
                ParentReference = new ItemReference
                {
                    DriveId = driveId,
                    Path = $"/root:/{_biblioteca}/{tipo}"
                }
            };

            await _graph.Drives[driveId].Items[itemIdPropuesta]
                .PatchAsync(destino);
        }
        catch (ODataError ex)
        {
            throw new SharePointException(
                ex.Error?.Message ?? "Error al mover archivo en SharePoint",
                (int?)ex.ResponseStatusCode);
        }
    }

    public async Task<string> ObtenerUrlDescargaAsync(string itemId)
    {
        try
        {
            var driveId = await GetDriveIdAsync();
            var body = new Microsoft.Graph.Drives.Item.Items.Item.CreateLink.CreateLinkPostRequestBody
            {
                Type = "view",
                Scope = "organization",
                ExpirationDateTime = DateTimeOffset.UtcNow.AddHours(1)
            };

            var permission = await _graph.Drives[driveId].Items[itemId]
                .CreateLink.PostAsync(body);

            return permission?.Link?.WebUrl
                ?? throw new SharePointException("No se pudo obtener URL de descarga");
        }
        catch (ODataError ex)
        {
            throw new SharePointException(
                ex.Error?.Message ?? "Error al obtener URL de descarga",
                (int?)ex.ResponseStatusCode);
        }
    }

    public async Task<string> ObtenerUrlEdicionAsync(string itemId, string nombreArchivo)
    {
        try
        {
            var driveId = await GetDriveIdAsync();
            var body = new Microsoft.Graph.Drives.Item.Items.Item.CreateLink.CreateLinkPostRequestBody
            {
                Type = "edit",
                Scope = "organization",
                ExpirationDateTime = DateTimeOffset.UtcNow.AddHours(8)
            };

            var permission = await _graph.Drives[driveId].Items[itemId]
                .CreateLink.PostAsync(body);

            return permission?.Link?.WebUrl
                ?? throw new SharePointException("No se pudo obtener URL de edición");
        }
        catch (ODataError ex)
        {
            throw new SharePointException(
                ex.Error?.Message ?? "Error al obtener URL de edición",
                (int?)ex.ResponseStatusCode);
        }
    }

    public async Task EliminarArchivoAsync(string itemId)
    {
        try
        {
            var driveId = await GetDriveIdAsync();
            await _graph.Drives[driveId].Items[itemId].DeleteAsync();
        }
        catch (ODataError ex)
        {
            throw new SharePointException(
                ex.Error?.Message ?? "Error al eliminar archivo en SharePoint",
                (int?)ex.ResponseStatusCode);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(string itemId, string url)> SubirAsync(Stream contenido, string ruta)
    {
        try
        {
            var driveId = await GetDriveIdAsync();
            var item = await _graph.Drives[driveId].Root
                .ItemWithPath(ruta)
                .Content.PutAsync(contenido);

            return (item!.Id!, item.WebUrl ?? string.Empty);
        }
        catch (ODataError ex)
        {
            throw new SharePointException(
                ex.Error?.Message ?? "Error al subir archivo a SharePoint",
                (int?)ex.ResponseStatusCode);
        }
    }

    private async Task<string> GetDriveIdAsync()
    {
        var drive = await _graph.Sites[_siteId].Drive.GetAsync();
        return drive?.Id ?? throw new SharePointException("No se pudo obtener el Drive");
    }
}
