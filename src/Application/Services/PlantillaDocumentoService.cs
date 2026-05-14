using System.Globalization;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Application.Services;

public class PlantillaDocumentoService(
    IPlantillaDocumentoRepository repository,
    IColaboradorRepository colaboradorRepository,
    IFileStorageService storage)
{
    private const string ContenedorDocx = "plantillas-docx";
    private const string ContenedorPdf  = "solicitudes-pdf";

    // ── Listado ──────────────────────────────────────────────────────────────

    public async Task<List<PlantillaDocumentoDto>> GetAllAsync()
    {
        var items = await repository.GetAllAsync();
        return items.Select(MapToDto).ToList();
    }

    public async Task<List<PlantillaDocumentoDto>> GetDisponiblesParaColaboradorAsync(string email)
    {
        var colaborador = await colaboradorRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Usuario no registrado como colaborador");

        var items = await repository.GetByAreaAsync(colaborador.AreaId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<PlantillaDocumentoDto?> GetByIdAsync(int id)
    {
        var p = await repository.GetByIdAsync(id);
        return p is null ? null : MapToDto(p);
    }

    // ── CRUD Admin ───────────────────────────────────────────────────────────

    public async Task<PlantillaDocumentoDto> CreateAsync(CreatePlantillaDocumentoDto dto)
    {
        string? docxFileKey = null;
        if (!string.IsNullOrWhiteSpace(dto.ArchivoDocxBase64))
        {
            var docxBytes = Convert.FromBase64String(dto.ArchivoDocxBase64);
            var nombreArchivo = $"{dto.Nombre.Replace(" ", "_")}.docx";
            using var ms = new MemoryStream(docxBytes);
            docxFileKey = await storage.UploadAsync(
                ms, nombreArchivo, ContenedorDocx,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        }

        var plantilla = new PlantillaDocumento
        {
            Nombre             = dto.Nombre,
            Descripcion        = dto.Descripcion,
            TipoPlantilla      = dto.TipoPlantilla == "docx" ? TipoPlantilla.Docx : TipoPlantilla.Html,
            ContenidoHtml      = dto.ContenidoHtml,
            DocxFileKey        = docxFileKey,
            FirmaImagenBase64  = dto.FirmaImagenBase64,
            NombreFirmante     = dto.NombreFirmante,
            CargoFirmante      = dto.CargoFirmante,
            AplicaTodasAreas   = dto.AplicaTodasAreas,
            VariablesEditables = dto.VariablesEditables.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(dto.VariablesEditables)
                : null,
            Areas = dto.AreaIds.Select(id => new PlantillaDocumentoArea { AreaId = id }).ToList()
        };

        var created = await repository.CreateAsync(plantilla);
        return MapToDto(created);
    }

    public async Task<PlantillaDocumentoDto?> UpdateAsync(int id, CreatePlantillaDocumentoDto dto)
    {
        var plantilla = await repository.GetByIdAsync(id);
        if (plantilla is null) return null;

        // Si llega un nuevo DOCX, subir al storage y eliminar el anterior
        if (!string.IsNullOrWhiteSpace(dto.ArchivoDocxBase64))
        {
            if (!string.IsNullOrWhiteSpace(plantilla.DocxFileKey))
                await storage.DeleteAsync(plantilla.DocxFileKey);

            var docxBytes = Convert.FromBase64String(dto.ArchivoDocxBase64);
            var nombreArchivo = $"{dto.Nombre.Replace(" ", "_")}.docx";
            using var ms = new MemoryStream(docxBytes);
            plantilla.DocxFileKey = await storage.UploadAsync(
                ms, nombreArchivo, ContenedorDocx,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            // Limpiar legacy si existía
            plantilla.ArchivoDocxLegacy = null;
        }

        plantilla.Nombre            = dto.Nombre;
        plantilla.Descripcion       = dto.Descripcion;
        plantilla.TipoPlantilla     = dto.TipoPlantilla == "docx" ? TipoPlantilla.Docx : TipoPlantilla.Html;
        plantilla.ContenidoHtml     = dto.ContenidoHtml;
        plantilla.FirmaImagenBase64 = dto.FirmaImagenBase64;
        plantilla.NombreFirmante    = dto.NombreFirmante;
        plantilla.CargoFirmante     = dto.CargoFirmante;
        plantilla.AplicaTodasAreas  = dto.AplicaTodasAreas;
        plantilla.VariablesEditables = dto.VariablesEditables.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(dto.VariablesEditables)
            : null;
        plantilla.Areas = dto.AreaIds.Select(aId => new PlantillaDocumentoArea
        {
            PlantillaDocumentoId = id,
            AreaId = aId
        }).ToList();

        var updated = await repository.UpdateAsync(plantilla);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await repository.GetByIdAsync(id);
        if (p is null) return false;

        if (!string.IsNullOrWhiteSpace(p.DocxFileKey))
            await storage.DeleteAsync(p.DocxFileKey);

        await repository.DeleteAsync(id);
        return true;
    }

    // ── Obtener bytes del DOCX (storage primero, legacy como fallback) ────────

    private async Task<byte[]> ObtenerDocxBytesAsync(PlantillaDocumento plantilla)
    {
        if (!string.IsNullOrWhiteSpace(plantilla.DocxFileKey))
        {
            var bytes = await storage.DownloadAsync(plantilla.DocxFileKey);
            if (bytes is { Length: > 0 }) return bytes;
        }

        // Fallback: binario legacy en SQL (datos viejos antes de la migración a storage)
        if (plantilla.ArchivoDocxLegacy is { Length: > 0 })
            return plantilla.ArchivoDocxLegacy;

        throw new InvalidOperationException("No se encontró el archivo DOCX de la plantilla.");
    }

    /// <summary>
    /// Devuelve el DOCX original y el diccionario de variables resueltas para el colaborador.
    /// </summary>
    public async Task<(byte[] docxBytes, PlantillaDocumento plantilla, Dictionary<string, string> variables)>
        ObtenerDocxConVariablesAsync(int plantillaId, string email, Dictionary<string, string>? extras = null)
    {
        var plantilla = await repository.GetByIdAsync(plantillaId)
            ?? throw new KeyNotFoundException("Plantilla no encontrada");
        var colaborador = await colaboradorRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Usuario no registrado como colaborador");

        if (!plantilla.AplicaTodasAreas &&
            !plantilla.Areas.Any(a => a.AreaId == colaborador.AreaId))
            throw new UnauthorizedAccessException("No tienes acceso a esta plantilla");

        var extrasFiltrados = FiltrarExtrasParaPlantilla(plantilla, extras, validarEditables: true);
        var variables = ConstruirVariablesDocumento(colaborador, plantilla, extrasFiltrados);
        var docxBytes = await ObtenerDocxBytesAsync(plantilla);

        return (docxBytes, plantilla, variables);
    }

    // ── Generación de documento ──────────────────────────────────────────────

    /// <summary>Resuelve variables sin registrar solicitud — para previsualización.</summary>
    public async Task<(string htmlResuelto, PlantillaDocumento plantilla, Dictionary<string, string> valoresPerfil)>
        PrevisualizarAsync(int plantillaId, string email, Dictionary<string, string>? extras = null, bool validarEditables = false)
    {
        var (html, plantilla, colaborador) = await ResolverInternoAsync(plantillaId, email, extras, validarEditables);

        var cultura = new CultureInfo("es-CO");
        var valoresPerfil = new Dictionary<string, string>
        {
            ["nombre_completo"]  = $"{colaborador.Nombre} {colaborador.Apellido}",
            ["nombre"]           = colaborador.Nombre,
            ["apellido"]         = colaborador.Apellido,
            ["cedula"]           = colaborador.Cedula ?? string.Empty,
            ["cargo"]            = colaborador.Cargo?.Nombre ?? string.Empty,
            ["area"]             = colaborador.Area?.Nombre ?? string.Empty,
            ["ciudad"]           = colaborador.Ciudad ?? string.Empty,
            ["fecha_expedicion"] = DateTime.Today.ToString("d 'de' MMMM 'de' yyyy", cultura),
            ["tipo_contrato"]    = colaborador.TipoContrato ?? string.Empty,
        };

        return (html, plantilla, valoresPerfil);
    }

    public async Task<(string htmlResuelto, PlantillaDocumento plantilla, Colaborador colaborador)>
        ResolverPlantillaAsync(int plantillaId, string email, Dictionary<string, string>? extras = null)
    {
        var (html, plantilla, colaborador) = await ResolverInternoAsync(plantillaId, email, extras, validarEditables: true);
        return (html, plantilla, colaborador);
    }

    /// <summary>
    /// Resuelve la plantilla para un colaborador específico por Id.
    /// Solo para uso del admin — no valida restricción de área.
    /// </summary>
    public async Task<(string htmlResuelto, PlantillaDocumento plantilla, Colaborador colaborador)>
        ResolverParaColaboradorAsync(int plantillaId, int colaboradorId)
    {
        var plantilla = await repository.GetByIdAsync(plantillaId)
            ?? throw new KeyNotFoundException("Plantilla no encontrada");

        var colaborador = await colaboradorRepository.GetByIdAsync(colaboradorId)
            ?? throw new KeyNotFoundException("Colaborador no encontrado");

        string contenidoResuelto;
        if (plantilla.TipoPlantilla == TipoPlantilla.Docx)
        {
            var docxBytes = await ObtenerDocxBytesAsync(plantilla);
            contenidoResuelto = ReemplazarVariablesEnDocx(docxBytes, colaborador, plantilla, null);
        }
        else
        {
            contenidoResuelto = ReemplazarVariables(plantilla.ContenidoHtml ?? string.Empty, colaborador, plantilla, null);
        }

        return (contenidoResuelto, plantilla, colaborador);
    }

    private async Task<(string htmlResuelto, PlantillaDocumento plantilla, Colaborador colaborador)>
        ResolverInternoAsync(int plantillaId, string email, Dictionary<string, string>? extras, bool validarEditables)
    {
        var plantilla = await repository.GetByIdAsync(plantillaId)
            ?? throw new KeyNotFoundException("Plantilla no encontrada");

        var colaborador = await colaboradorRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Usuario no registrado como colaborador");

        if (!plantilla.AplicaTodasAreas &&
            !plantilla.Areas.Any(a => a.AreaId == colaborador.AreaId))
            throw new UnauthorizedAccessException("No tienes acceso a esta plantilla");

        var extrasFiltrados = FiltrarExtrasParaPlantilla(plantilla, extras, validarEditables);

        string contenidoResuelto;
        if (plantilla.TipoPlantilla == TipoPlantilla.Docx)
        {
            var docxBytes = await ObtenerDocxBytesAsync(plantilla);
            contenidoResuelto = ReemplazarVariablesEnDocx(docxBytes, colaborador, plantilla, extrasFiltrados);
        }
        else
        {
            contenidoResuelto = ReemplazarVariables(plantilla.ContenidoHtml ?? string.Empty, colaborador, plantilla, extrasFiltrados);
        }

        return (contenidoResuelto, plantilla, colaborador);
    }

    // ── Solicitudes con flujo de aprobación ──────────────────────────────────

    public async Task<SolicitudDocumentoDto> EnviarSolicitudAsync(int plantillaId, string email, byte[] pdfBytes)
    {
        var colaborador = await colaboradorRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Usuario no registrado como colaborador");

        if (await repository.ExisteSolicitudPendienteAsync(plantillaId, colaborador.Id))
            throw new InvalidOperationException("Ya tienes una solicitud pendiente para esta carta.");

        // Subir PDF al storage — validación de MIME y tamaño la hace IFileStorageService
        var nombrePdf = $"solicitud_{plantillaId}_{colaborador.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        using var ms = new MemoryStream(pdfBytes);
        var pdfFileKey = await storage.UploadAsync(ms, nombrePdf, ContenedorPdf, "application/pdf");

        var solicitud = new SolicitudDocumento
        {
            PlantillaDocumentoId  = plantillaId,
            ColaboradorId         = colaborador.Id,
            FechaSolicitud        = DateTime.UtcNow,
            Estado                = EstadoSolicitud.Pendiente,
            PdfFileKey            = pdfFileKey,
            NotificadoColaborador = true,
        };

        await repository.CreateSolicitudAsync(solicitud);

        var plantilla = await repository.GetByIdAsync(plantillaId);
        solicitud.Colaborador = colaborador;
        solicitud.PlantillaDocumento = plantilla!;
        return MapSolicitud(solicitud);
    }

    public async Task<SolicitudDocumentoDto?> AprobarSolicitudAsync(int solicitudId, string? comentario)
    {
        var s = await repository.GetSolicitudByIdAsync(solicitudId);
        if (s is null) return null;
        s.Estado                = EstadoSolicitud.Aprobada;
        s.ComentarioAdmin       = comentario;
        s.FechaResolucion       = DateTime.UtcNow;
        s.NotificadoColaborador = false;
        await repository.UpdateSolicitudAsync(s);
        return MapSolicitud(s);
    }

    public async Task<SolicitudDocumentoDto?> RechazarSolicitudAsync(int solicitudId, string? comentario)
    {
        var s = await repository.GetSolicitudByIdAsync(solicitudId);
        if (s is null) return null;
        s.Estado                = EstadoSolicitud.Rechazada;
        s.ComentarioAdmin       = comentario;
        s.FechaResolucion       = DateTime.UtcNow;
        s.NotificadoColaborador = false;
        await repository.UpdateSolicitudAsync(s);
        return MapSolicitud(s);
    }

    public async Task MarcarComoVistasAsync(string email)
    {
        var colaborador = await colaboradorRepository.GetByEmailAsync(email);
        if (colaborador is null) return;
        await repository.MarcarSolicitudesComoVistaAsync(colaborador.Id);
    }

    /// <summary>
    /// Descarga el PDF de una solicitud aprobada desde storage (con fallback a legacy SQL).
    /// </summary>
    public async Task<byte[]?> DescargarSolicitudAprobadaAsync(int solicitudId, string emailSolicitante)
    {
        var s = await repository.GetSolicitudByIdAsync(solicitudId);
        if (s is null) return null;

        if (s.Colaborador.Email != emailSolicitante)
            throw new UnauthorizedAccessException("No tienes acceso a esta solicitud");
        if (s.Estado != EstadoSolicitud.Aprobada)
            throw new InvalidOperationException("La solicitud no está aprobada");

        return await ObtenerPdfSolicitudAsync(s);
    }

    public async Task<List<SolicitudDocumentoDto>> GetSolicitudesAsync(string email)
    {
        var colaborador = await colaboradorRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Usuario no registrado como colaborador");
        var solicitudes = await repository.GetSolicitudesByColaboradorAsync(colaborador.Id);
        return solicitudes.Select(MapSolicitud).ToList();
    }

    public async Task<List<SolicitudDocumentoDto>> GetTodasSolicitudesAsync()
    {
        var solicitudes = await repository.GetTodasSolicitudesAsync();
        return solicitudes.Select(MapSolicitud).ToList();
    }

    public async Task<List<SolicitudDocumentoDto>> GetPendientesAsync()
    {
        var solicitudes = await repository.GetSolicitudesPendientesAsync();
        return solicitudes.Select(MapSolicitud).ToList();
    }

    public Task<int> CountPendientesAsync() => repository.CountPendientesAsync();

    public async Task<SolicitudDocumento?> GetSolicitudEntityAsync(int id) =>
        await repository.GetSolicitudByIdAsync(id);

    /// <summary>
    /// Devuelve el PDF de una solicitud si el solicitante tiene acceso.
    /// Admin/jefe puede ver cualquiera; colaborador solo la suya.
    /// </summary>
    public async Task<byte[]?> GetPdfSolicitudAsync(int solicitudId, string email)
    {
        var s = await repository.GetSolicitudByIdAsync(solicitudId);
        if (s is null) return null;

        if (s.Colaborador.Email != email)
        {
            var solicitante = await colaboradorRepository.GetByEmailAsync(email);
            if (solicitante is null ||
                (solicitante.Rol != RolUsuario.Jefe && solicitante.Rol != RolUsuario.Admin))
                throw new UnauthorizedAccessException("No tienes acceso a este PDF.");
        }

        return await ObtenerPdfSolicitudAsync(s);
    }

    // ── Helpers privados ─────────────────────────────────────────────────────

    private async Task<byte[]?> ObtenerPdfSolicitudAsync(SolicitudDocumento s)
    {
        // 1. Storage (nuevo flujo)
        if (!string.IsNullOrWhiteSpace(s.PdfFileKey))
        {
            var bytes = await storage.DownloadAsync(s.PdfFileKey);
            if (bytes is { Length: > 0 }) return bytes;
        }

        // 2. Fallback: binario legacy en SQL
        return s.PdfBytes;
    }

    private static SolicitudDocumentoDto MapSolicitud(SolicitudDocumento s) => new()
    {
        Id                   = s.Id,
        PlantillaDocumentoId = s.PlantillaDocumentoId,
        PlantillaNombre      = s.PlantillaDocumento?.Nombre ?? string.Empty,
        ColaboradorId        = s.ColaboradorId,
        ColaboradorNombre    = $"{s.Colaborador.Nombre} {s.Colaborador.Apellido}",
        ColaboradorEmail     = s.Colaborador.Email ?? string.Empty,
        FechaSolicitud       = s.FechaSolicitud,
        Estado               = s.Estado.ToString(),
        ComentarioAdmin      = s.ComentarioAdmin,
        FechaResolucion      = s.FechaResolucion,
        TienePdf             = s.PdfFileKey is not null || s.PdfBytes is not null,
        TieneNovedad         = !s.NotificadoColaborador,
    };

    private static string TextoGeneroParaDocumento(GeneroColaborador g) => g switch
    {
        GeneroColaborador.Masculino => "el señor",
        GeneroColaborador.Femenino  => "la señora",
        _                           => "el(la) señor(a)",
    };

    private static Dictionary<string, string>? FiltrarExtrasParaPlantilla(
        PlantillaDocumento plantilla,
        Dictionary<string, string>? extras,
        bool validarEditables)
    {
        if (extras is null || extras.Count == 0) return null;

        var sinReservados = extras
            .Where(kv => !string.Equals(kv.Key, "genero", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (!validarEditables) return sinReservados;

        var permitidas = string.IsNullOrWhiteSpace(plantilla.VariablesEditables)
            ? []
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(plantilla.VariablesEditables) ?? [];

        var filtrados = sinReservados
            .Where(kv => permitidas.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return filtrados.Count == 0 ? null : filtrados;
    }

    private static Dictionary<string, string> ConstruirVariablesDocumento(
        Colaborador c, PlantillaDocumento p, Dictionary<string, string>? extrasFiltrados)
    {
        var cultura = new CultureInfo("es-CO");
        var hoy = DateTime.Today;
        var variables = new Dictionary<string, string>
        {
            ["{{nombre_completo}}"]  = $"{c.Nombre} {c.Apellido}",
            ["{{nombre}}"]           = c.Nombre,
            ["{{apellido}}"]         = c.Apellido,
            ["{{cedula}}"]           = c.Cedula ?? string.Empty,
            ["{{cargo}}"]            = c.Cargo?.Nombre ?? string.Empty,
            ["{{area}}"]             = c.Area?.Nombre ?? string.Empty,
            ["{{fecha_ingreso}}"]    = c.FechaIngreso.ToString("d 'de' MMMM 'de' yyyy", cultura),
            ["{{tipo_contrato}}"]    = c.TipoContrato ?? string.Empty,
            ["{{sueldo_basico}}"]    = c.SueldoBasico.HasValue
                ? c.SueldoBasico.Value.ToString("C0", cultura) : string.Empty,
            ["{{ciudad}}"]           = c.Ciudad ?? string.Empty,
            ["{{fecha_expedicion}}"] = hoy.ToString("d 'de' MMMM 'de' yyyy", cultura),
            ["{{nombre_firmante}}"]  = p.NombreFirmante ?? string.Empty,
            ["{{cargo_firmante}}"]   = p.CargoFirmante ?? string.Empty,
            ["{{genero}}"]           = TextoGeneroParaDocumento(c.Genero),
        };

        if (extrasFiltrados is { Count: > 0 })
        {
            foreach (var (key, value) in extrasFiltrados)
                variables[$"{{{{{key}}}}}"] = value;
        }

        return variables;
    }

    private static string ReemplazarVariables(
        string html, Colaborador c, PlantillaDocumento p, Dictionary<string, string>? extrasFiltrados = null)
    {
        var variables = ConstruirVariablesDocumento(c, p, extrasFiltrados);
        var resultado = html;
        foreach (var (key, value) in variables)
            resultado = resultado.Replace(key, value);
        return resultado;
    }

    private static string ReemplazarVariablesEnDocx(
        byte[] docxBytes, Colaborador c, PlantillaDocumento p, Dictionary<string, string>? extrasFiltrados = null)
    {
        var variables = ConstruirVariablesDocumento(c, p, extrasFiltrados);
        return System.Text.Json.JsonSerializer.Serialize(new DocxReemplazoPayload
        {
            DocxBase64 = Convert.ToBase64String(docxBytes),
            Variables  = variables
        });
    }

    private static PlantillaDocumentoDto MapToDto(PlantillaDocumento p) => new()
    {
        Id                 = p.Id,
        Nombre             = p.Nombre,
        Descripcion        = p.Descripcion,
        TipoPlantilla      = p.TipoPlantilla == TipoPlantilla.Docx ? "docx" : "html",
        ContenidoHtml      = p.ContenidoHtml,
        TieneDocx          = p.DocxFileKey is not null || p.ArchivoDocxLegacy is { Length: > 0 },
        FirmaImagenBase64  = p.FirmaImagenBase64,
        NombreFirmante     = p.NombreFirmante,
        CargoFirmante      = p.CargoFirmante,
        AplicaTodasAreas   = p.AplicaTodasAreas,
        AreaIds            = p.Areas.Select(a => a.AreaId).ToList(),
        AreaNombres        = p.Areas.Select(a => a.Area?.Nombre ?? string.Empty).ToList(),
        VariablesEditables = string.IsNullOrWhiteSpace(p.VariablesEditables)
            ? []
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.VariablesEditables) ?? [],
    };
}

/// <summary>Payload serializado que pasa Application → Infrastructure para procesar docx</summary>
public record DocxReemplazoPayload
{
    public string DocxBase64 { get; init; } = string.Empty;
    public Dictionary<string, string> Variables { get; init; } = [];
}
