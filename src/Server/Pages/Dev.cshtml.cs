using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TalentManagement.Server.Services;

namespace TalentManagement.Server.Pages;

public class DevModel(DevUserStore store) : PageModel
{
    public record UserOption(string Email, string Label, string Icon);

    public UserOption[] Users { get; } =
    [
        new("dev.colaborador@test.local", "Dev Colaborador", "👤"),
        new("dev.jefe@test.local",        "Dev Jefe Tech",   "👔"),
        new("dev.jeferrhh@test.local",    "Dev Jefe RRHH",   "🧑‍💼"),
    ];

    public string? ActiveEmail => store.ActiveEmail;

    public void OnGet() { }

    public IActionResult OnPostSetUser(string email)
    {
        if (Users.Any(u => u.Email == email))
            store.SetUser(email);
        return RedirectToPage();
    }

    public IActionResult OnPostUseReal()
    {
        store.ClearUser();
        return RedirectToPage();
    }
}
