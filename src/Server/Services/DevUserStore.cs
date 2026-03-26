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

    public string? ActiveEmail { get; private set; } = KnownUsers[0];

    public IReadOnlyList<string> Users => KnownUsers;

    public void SetUser(string email) => ActiveEmail = email;

    public void ClearUser() => ActiveEmail = null; // null = usar token real de MSAL
}
