using System.Net.Http.Json;

namespace TalentManagement.Client.Services;

public class AuthInfoService(HttpClient http)
{
    public bool EsMicrosoftUser { get; private set; }
    public bool EsColaborador { get; private set; }
    public bool EsSoloColaborador { get; private set; }
    public bool EsJefe { get; private set; }
    public bool EsAdmin { get; private set; }
    public bool PuedeResolverSolicitudes { get; private set; }
    public int? ColaboradorId { get; private set; }
    public int? AreaId { get; private set; }
    public string Rol { get; private set; } = "Colaborador";
    public string Nombre { get; private set; } = "Usuario";
    public string Email { get; private set; } = string.Empty;

    public string Iniciales
    {
        get
        {
            var parts = Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : Nombre.Length > 0 ? Nombre[0..1].ToUpper() : "U";
        }
    }

    private bool _loaded;

    public async Task LoadAsync()
    {
        if (_loaded) return;
        try
        {
            var perfil = await http.GetFromJsonAsync<PerfilResponse>("api/v1/auth/perfil");
            if (perfil is not null)
            {
                EsMicrosoftUser = !perfil.EsDevUser;
                EsColaborador = perfil.EsColaborador;
                Rol = perfil.Rol ?? "Colaborador";
                EsJefe = Rol == "Jefe";
                EsAdmin = Rol == "Admin";
                EsSoloColaborador = Rol == "Colaborador";
                ColaboradorId = perfil.ColaboradorId;
                AreaId = perfil.AreaId;
                Nombre = perfil.Nombre ?? perfil.Email ?? "Usuario";
                Email = perfil.Email ?? string.Empty;
                PuedeResolverSolicitudes = perfil.PuedeResolverSolicitudes
                    ?? (EsJefe || EsAdmin);
                // Solo marcar como cargado si obtuvimos datos reales
                _loaded = true;
            }
        }
        catch
        {
            // No marcar _loaded=true para que se reintente en el proximo render
            EsMicrosoftUser = false;
            EsColaborador = false;
            EsSoloColaborador = true;
            EsAdmin = false;
            EsJefe = false;
            Rol = "Colaborador";
            PuedeResolverSolicitudes = false;
        }
    }

    public void Invalidar() => _loaded = false;

    private record PerfilResponse(
        string Email,
        bool EsColaborador,
        bool EsJefe,
        int? ColaboradorId,
        int? AreaId = null,
        string? Nombre = null,
        string? Rol = null,
        bool EsDevUser = false,
        bool? PuedeResolverSolicitudes = null);
}
