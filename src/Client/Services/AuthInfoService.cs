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
    public string Rol { get; private set; } = "Admin";
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
                Rol = perfil.Rol ?? "Admin";
                EsJefe = Rol == "Jefe";
                EsAdmin = Rol == "Admin";
                EsSoloColaborador = perfil.EsColaborador && Rol == "Colaborador";
                ColaboradorId = perfil.ColaboradorId;
                PuedeResolverSolicitudes = perfil.PuedeResolverSolicitudes
                    ?? (EsMicrosoftUser || EsJefe || EsAdmin);
            }
        }
        catch
        {
            EsMicrosoftUser = false;
            EsSoloColaborador = false;
            EsAdmin = true;
            PuedeResolverSolicitudes = true;
        }
        _loaded = true;
    }

    public void Invalidar() => _loaded = false;

    private record PerfilResponse(
        string Email,
        bool EsColaborador,
        bool EsJefe,
        int? ColaboradorId,
        string? Rol = null,
        bool EsDevUser = false,
        bool? PuedeResolverSolicitudes = null);
}
