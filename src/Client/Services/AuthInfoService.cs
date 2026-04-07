using System.Net.Http.Json;

namespace TalentManagement.Client.Services;

/// <summary>
/// Cachea el perfil del usuario actual: si es Microsoft, si es colaborador, si es jefe.
/// Se consulta una vez al iniciar la app.
/// </summary>
public class AuthInfoService(HttpClient http)
{
    public bool EsMicrosoftUser { get; private set; }
    public bool EsSoloColaborador { get; private set; }
    public int? ColaboradorId { get; private set; }
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
                // Es "solo colaborador" si está en la BD y no es jefe de área
                EsSoloColaborador = perfil.EsColaborador && !perfil.EsJefe;
                ColaboradorId = perfil.ColaboradorId;
            }
        }
        catch
        {
            EsMicrosoftUser = false;
            EsSoloColaborador = false;
        }
        _loaded = true;
    }

    private record PerfilResponse(
        string Email,
        bool EsColaborador,
        bool EsJefe,
        int? ColaboradorId,
        bool EsDevUser = false);
}
