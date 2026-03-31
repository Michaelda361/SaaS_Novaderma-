using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TalentManagement.Server.Services;

namespace TalentManagement.Server.Pages;

[IgnoreAntiforgeryToken]
public class DevModel(DevUserStore store) : PageModel
{
    public record UserOption(string Email, string Label, string Icon);

    public UserOption[] Users { get; } =
    [
        new("dev.colaborador@test.local", "Andrés Martínez",   "👤"),
        new("dev.jefe@test.local",        "Carlos Herrera",    "👔"),
        new("dev.jeferrhh@test.local",    "Francy Gutiérrez",  "🧑‍💼"),
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
