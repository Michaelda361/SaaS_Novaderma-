namespace TalentManagement.Server.Services;

/// <summary>
/// Singleton que guarda el email del usuario dev activo en memoria.
/// Solo se usa en Development. Permite cambiar de rol sin reiniciar.
/// </summary>
public class DevUserStore
{
    private static readonly string[] KnownUsers =
    [
        "dev.colaborador@test.local",
        "dev.jefe@test.local",
        "dev.jeferrhh@test.local",
    ];

    public string? ActiveEmail { get; private set; }

    public IReadOnlyList<string> Users => KnownUsers;

    public DevUserStore(IConfiguration config)
    {
        // Arranca con el usuario definido en appsettings.Development.json
        ActiveEmail = config["DevSettings:DefaultDevUser"];
    }

    public void SetUser(string email) => ActiveEmail = email;

    public void ClearUser() => ActiveEmail = null;
}
