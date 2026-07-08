using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace TalentManagement.Client.Services;

public class MockAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Simulamos un usuario administrador de desarrollo por defecto
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "dev.jeferrhh@test.local"),
            new Claim(ClaimTypes.Email, "dev.jeferrhh@test.local"),
            new Claim("preferred_username", "dev.jeferrhh@test.local"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "MockAuth");

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}
