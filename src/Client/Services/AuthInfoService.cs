using System.Net.Http.Json;

namespace TalentManagement.Client.Services;

public class AuthInfoService(HttpClient http)
{
    public bool EsMicrosoftUser { get; private set; }
    public bool EsSoloColaborador { get; private set; }
    public bool EsJefe { get; private set; }
    public bool EsAdmin { get; private set; }
    public bool PuedeResolverSolicitudes { get; private set; }
    public int? ColaboradorId { get; private set; }
    public string Rol { get; private set; } = "Colaborador";
    public string Nombre { get; private set; } = "Usuario";
    public string Email { get; private set; } = string.Empty;
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
                Rol = perfil.Rol ?? "Colaborador";
                EsJefe = Rol == "Jefe";
                EsAdmin = Rol == "Admin";
                EsSoloColaborador = Rol == "Colaborador";
                ColaboradorId = perfil.ColaboradorId;
                Nombre = perfil.Nombre ?? perfil.Email ?? "Usuario";
                Email = perfil.Email ?? string.Empty;
                PuedeResolverSolicitudes = perfil.PuedeResolverSolicitudes
                    ?? (EsJefe || EsAdmin);
            }
        }
        catch
        {
            // Error de red: mínimo privilegio, no admin
            EsMicrosoftUser = false;
            EsSoloColaborador = true;
            EsAdmin = false;
            EsJefe = false;
            Rol = "Colaborador";
            PuedeResolverSolicitudes = false;
            Nombre = "Usuario";
        }
        _loaded = true;
    }

    public void Invalidar() => _loaded = false;

    private record PerfilResponse(
        string Email,
        bool EsColaborador,
        bool EsJefe,
        int? ColaboradorId,
        string? Nombre = null,
        string? Rol = null,
        bool EsDevUser = false,
        bool? PuedeResolverSolicitudes = null);
}
