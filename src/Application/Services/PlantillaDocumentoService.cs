using System.Globalization;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Application.Services;

public class PlantillaDocumentoService(
    IPlantillaDocumentoRepository repository,
    IColaboradorRepository colaboradorRepository)
{
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
        var plantilla = new PlantillaDocumento
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            TipoPlantilla = dto.TipoPlantilla == "docx" ? TipoPlantilla.Docx : TipoPlantilla.Html,
            ContenidoHtml = dto.ContenidoHtml,
            ArchivoDocx = string.IsNullOrWhiteSpace(dto.ArchivoDocxBase64)
                ? null
                : Convert.FromBase64String(dto.ArchivoDocxBase64),
            FirmaImagenBase64 = dto.FirmaImagenBase64,
            NombreFirmante = dto.NombreFirmante,
            CargoFirmante = dto.CargoFirmante,
            AplicaTodasAreas = dto.AplicaTodasAreas,
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

        plantilla.Nombre = dto.Nombre;
        plantilla.Descripcion = dto.Descripcion;
        plantilla.TipoPlantilla = dto.TipoPlantilla == "docx" ? TipoPlantilla.Docx : TipoPlantilla.Html;
        plantilla.ContenidoHtml = dto.ContenidoHtml;
        if (!string.IsNullOrWhiteSpace(dto.ArchivoDocxBase64))
            plantilla.ArchivoDocx = Convert.FromBase64String(dto.ArchivoDocxBase64);
        plantilla.FirmaImagenBase64 = dto.FirmaImagenBase64;
        plantilla.NombreFirmante = dto.NombreFirmante;
        plantilla.CargoFirmante = dto.CargoFirmante;
        plantilla.AplicaTodasAreas = dto.AplicaTodasAreas;
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
        await repository.DeleteAsync(id);
        return true;
    }

    /// <summary>Registra una solicitud de descarga sin generar el documento.</summary>
    public async Task RegistrarSolicitudAsync(int plantillaId, string email)
    {
        var colaborador = await colaboradorRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Usuario no registrado como colaborador");
        await repository.CreateSolicitudAsync(new SolicitudDocumento
        {
            PlantillaDocumentoId = plantillaId,
            ColaboradorId = colaborador.Id,
            FechaSolicitud = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Devuelve el DOCX original y el diccionario de variables resueltas para el colaborador.
    /// </summary>
    public async Task<(byte[] docxBytes, PlantillaDocumento plantilla, Dictionary<string, string> variables)>
        ObtenerDocxConVariablesAsync(int plantillaId, string email)
    {
        var plantilla = await repository.GetByIdAsync(plantillaId)
            ?? throw new KeyNotFoundException("Plantilla no encontrada");
        var colaborador = await colaboradorRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Usuario no registrado como colaborador");

        if (!plantilla.AplicaTodasAreas &&
            !plantilla.Areas.Any(a => a.AreaId == colaborador.AreaId))
            throw new UnauthorizedAccessException("No tienes acceso a esta plantilla");

        var cultura = new CultureInfo("es-CO");
        var hoy = DateTime.Today;
        var variables = new Dictionary<string, string>
        {
            ["{{nombre_completo}}"] = $"{colaborador.Nombre} {colaborador.Apellido}",
            ["{{nombre}}"]          = colaborador.Nombre,
            ["{{apellido}}"]        = colaborador.Apellido,
            ["{{cedula}}"]          = colaborador.Cedula ?? string.Empty,
            ["{{cargo}}"]           = colaborador.Cargo?.Nombre ?? string.Empty,
            ["{{area}}"]            = colaborador.Area?.Nombre ?? string.Empty,
            ["{{fecha_ingreso}}"]   = colaborador.FechaIngreso.ToString("d 'de' MMMM 'de' yyyy", cultura),
            ["{{tipo_contrato}}"]   = colaborador.TipoContrato ?? string.Empty,
            ["{{sueldo_basico}}"]   = colaborador.SueldoBasico.HasValue
                ? colaborador.SueldoBasico.Value.ToString("C0", cultura) : string.Empty,
            ["{{ciudad}}"]          = colaborador.Ciudad ?? string.Empty,
            ["{{fecha_expedicion}}"]= hoy.ToString("d 'de' MMMM 'de' yyyy", cultura),
            ["{{nombre_firmante}}"] = plantilla.NombreFirmante ?? string.Empty,
            ["{{cargo_firmante}}"]  = plantilla.CargoFirmante ?? string.Empty,
        };

        return (plantilla.ArchivoDocx!, plantilla, variables);
    }

    // ── Generación de documento ──────────────────────────────────────────────

    /// <summary>Resuelve variables sin registrar solicitud — para previsualización.</summary>
    public async Task<(string htmlResuelto, PlantillaDocumento plantilla, Dictionary<string, string> valoresPerfil)>
        PrevisualizarAsync(int plantillaId, string email, Dictionary<string, string>? extras = null)
    {
        var (html, plantilla, colaborador) = await ResolverInternoAsync(plantillaId, email, extras, validarEditables: false);

        // Valores del perfil para pre-rellenar campos editables en el cliente
        var cultura = new System.Globalization.CultureInfo("es-CO");
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

        // Filtrar extras: solo se aplican las variables marcadas como editables
        Dictionary<string, string>? extrasFiltrados = null;
        if (extras is { Count: > 0 })
        {
            var editablesPermitidas = string.IsNullOrWhiteSpace(plantilla.VariablesEditables)
                ? []
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(plantilla.VariablesEditables) ?? [];

            extrasFiltrados = validarEditables
                ? extras.Where(kv => editablesPermitidas.Contains(kv.Key))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)
                : extras; // en preview se permite todo para que el admin pueda probar
        }

        var contenidoResuelto = plantilla.TipoPlantilla == TipoPlantilla.Docx
            ? ReemplazarVariablesEnDocx(plantilla.ArchivoDocx!, colaborador, plantilla, extrasFiltrados)
            : ReemplazarVariables(plantilla.ContenidoHtml ?? string.Empty, colaborador, plantilla, extrasFiltrados);

        return (contenidoResuelto, plantilla, colaborador);
    }

    // ── Solicitudes con flujo de aprobación ──────────────────────────────────

    public async Task<SolicitudDocumentoDto> EnviarSolicitudAsync(int plantillaId, string email, byte[] pdfBytes)
    {
        var colaborador = await colaboradorRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Usuario no registrado como colaborador");

        // Prevenir solicitudes duplicadas
        if (await repository.ExisteSolicitudPendienteAsync(plantillaId, colaborador.Id))
            throw new InvalidOperationException("Ya tienes una solicitud pendiente para esta carta.");

        var solicitud = new SolicitudDocumento
        {
            PlantillaDocumentoId = plantillaId,
            ColaboradorId = colaborador.Id,
            FechaSolicitud = DateTime.UtcNow,
            Estado = Domain.Enums.EstadoSolicitud.Pendiente,
            PdfBytes = pdfBytes,
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
        s.Estado = Domain.Enums.EstadoSolicitud.Aprobada;
        s.ComentarioAdmin = comentario;
        s.FechaResolucion = DateTime.UtcNow;
        s.NotificadoColaborador = false;
        await repository.UpdateSolicitudAsync(s);
        return MapSolicitud(s);
    }

    public async Task<SolicitudDocumentoDto?> RechazarSolicitudAsync(int solicitudId, string? comentario)
    {
        var s = await repository.GetSolicitudByIdAsync(solicitudId);
        if (s is null) return null;
        s.Estado = Domain.Enums.EstadoSolicitud.Rechazada;
        s.ComentarioAdmin = comentario;
        s.FechaResolucion = DateTime.UtcNow;
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

    public async Task<byte[]?> DescargarSolicitudAprobadaAsync(int solicitudId, string emailSolicitante)
    {
        var s = await repository.GetSolicitudByIdAsync(solicitudId);
        if (s is null) return null;
        if (s.Colaborador.Email != emailSolicitante)
            throw new UnauthorizedAccessException("No tienes acceso a esta solicitud");
        if (s.Estado != Domain.Enums.EstadoSolicitud.Aprobada)
            throw new InvalidOperationException("La solicitud no está aprobada");
        return s.PdfBytes;
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

    public async Task<Domain.Entities.SolicitudDocumento?> GetSolicitudEntityAsync(int id) =>
        await repository.GetSolicitudByIdAsync(id);

    /// <summary>
    /// Devuelve el PDF de una solicitud si el solicitante tiene acceso.
    /// Admin/jefe puede ver cualquiera; colaborador solo la suya.
    /// </summary>
    public async Task<byte[]?> GetPdfSolicitudAsync(int solicitudId, string email)
    {
        var s = await repository.GetSolicitudByIdAsync(solicitudId);
        if (s?.PdfBytes is null) return null;

        if (s.Colaborador.Email == email) return s.PdfBytes;

        var solicitante = await colaboradorRepository.GetByEmailAsync(email);
        if (solicitante is null) return s.PdfBytes;
        if (solicitante.Rol == Domain.Enums.RolUsuario.Jefe
            || solicitante.Rol == Domain.Enums.RolUsuario.Admin)
            return s.PdfBytes;

        throw new UnauthorizedAccessException("No tienes acceso a este PDF.");
    }

    private static SolicitudDocumentoDto MapSolicitud(SolicitudDocumento s) => new()
    {
        Id = s.Id,
        PlantillaDocumentoId = s.PlantillaDocumentoId,
        PlantillaNombre = s.PlantillaDocumento?.Nombre ?? string.Empty,
        ColaboradorId = s.ColaboradorId,
        ColaboradorNombre = $"{s.Colaborador.Nombre} {s.Colaborador.Apellido}",
        ColaboradorEmail = s.Colaborador.Email ?? string.Empty,
        FechaSolicitud = s.FechaSolicitud,
        Estado = s.Estado.ToString(),
        ComentarioAdmin = s.ComentarioAdmin,
        FechaResolucion = s.FechaResolucion,
        TienePdf = s.PdfBytes is not null,
        TieneNovedad = !s.NotificadoColaborador,
    };

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ReemplazarVariables(
        string html, Colaborador c, PlantillaDocumento p, Dictionary<string, string>? extras = null)
    {
        var cultura = new CultureInfo("es-CO");
        var hoy = DateTime.Today;

        var resultado = html
            .Replace("{{nombre_completo}}", $"{c.Nombre} {c.Apellido}")
            .Replace("{{nombre}}", c.Nombre)
            .Replace("{{apellido}}", c.Apellido)
            .Replace("{{cedula}}", c.Cedula ?? string.Empty)
            .Replace("{{cargo}}", c.Cargo?.Nombre ?? string.Empty)
            .Replace("{{area}}", c.Area?.Nombre ?? string.Empty)
            .Replace("{{fecha_ingreso}}", c.FechaIngreso.ToString("d 'de' MMMM 'de' yyyy", cultura))
            .Replace("{{tipo_contrato}}", c.TipoContrato ?? string.Empty)
            .Replace("{{sueldo_basico}}", c.SueldoBasico.HasValue
                ? c.SueldoBasico.Value.ToString("C0", cultura)
                : string.Empty)
            .Replace("{{ciudad}}", c.Ciudad ?? string.Empty)
            .Replace("{{fecha_expedicion}}", hoy.ToString("d 'de' MMMM 'de' yyyy", cultura))
            .Replace("{{nombre_firmante}}", p.NombreFirmante ?? string.Empty)
            .Replace("{{cargo_firmante}}", p.CargoFirmante ?? string.Empty);

        if (extras is { Count: > 0 })
            foreach (var (key, value) in extras)
                resultado = resultado.Replace($"{{{{{key}}}}}", value);

        return resultado;
    }

    private static string ReemplazarVariablesEnDocx(
        byte[] docxBytes, Colaborador c, PlantillaDocumento p, Dictionary<string, string>? extras = null)
    {
        var cultura = new CultureInfo("es-CO");
        var hoy = DateTime.Today;

        var variables = new Dictionary<string, string>
        {
            ["{{nombre_completo}}"] = $"{c.Nombre} {c.Apellido}",
            ["{{nombre}}"]          = c.Nombre,
            ["{{apellido}}"]        = c.Apellido,
            ["{{cedula}}"]          = c.Cedula ?? string.Empty,
            ["{{cargo}}"]           = c.Cargo?.Nombre ?? string.Empty,
            ["{{area}}"]            = c.Area?.Nombre ?? string.Empty,
            ["{{fecha_ingreso}}"]   = c.FechaIngreso.ToString("d 'de' MMMM 'de' yyyy", cultura),
            ["{{tipo_contrato}}"]   = c.TipoContrato ?? string.Empty,
            ["{{sueldo_basico}}"]   = c.SueldoBasico.HasValue
                ? c.SueldoBasico.Value.ToString("C0", cultura) : string.Empty,
            ["{{ciudad}}"]          = c.Ciudad ?? string.Empty,
            ["{{fecha_expedicion}}"]= hoy.ToString("d 'de' MMMM 'de' yyyy", cultura),
            ["{{nombre_firmante}}"] = p.NombreFirmante ?? string.Empty,
            ["{{cargo_firmante}}"]  = p.CargoFirmante ?? string.Empty,
        };

        if (extras is { Count: > 0 })
            foreach (var (key, value) in extras)
                variables[$"{{{{{key}}}}}"] = value;

        return System.Text.Json.JsonSerializer.Serialize(new DocxReemplazoPayload
        {
            DocxBase64 = Convert.ToBase64String(docxBytes),
            Variables = variables
        });
    }

    private static PlantillaDocumentoDto MapToDto(PlantillaDocumento p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        TipoPlantilla = p.TipoPlantilla == TipoPlantilla.Docx ? "docx" : "html",
        ContenidoHtml = p.ContenidoHtml,
        TieneDocx = p.ArchivoDocx is { Length: > 0 },
        FirmaImagenBase64 = p.FirmaImagenBase64,
        NombreFirmante = p.NombreFirmante,
        CargoFirmante = p.CargoFirmante,
        AplicaTodasAreas = p.AplicaTodasAreas,
        AreaIds = p.Areas.Select(a => a.AreaId).ToList(),
        AreaNombres = p.Areas.Select(a => a.Area?.Nombre ?? string.Empty).ToList(),
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
