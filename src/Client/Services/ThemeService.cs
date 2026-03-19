using Microsoft.JSInterop;

namespace TalentManagement.Client.Services;

public class ThemeService(IJSRuntime js)
{
    private bool _isDark = false;
    public bool IsDark => _isDark;

    public event Action? OnChange;

    public async Task InitAsync()
    {
        var saved = await js.InvokeAsync<string?>("localStorage.getItem", "theme");
        _isDark = saved == "dark";
        await ApplyAsync();
    }

    public async Task ToggleAsync()
    {
        _isDark = !_isDark;
        await ApplyAsync();
        await js.InvokeVoidAsync("localStorage.setItem", "theme", _isDark ? "dark" : "light");
        OnChange?.Invoke();
    }

    private async Task ApplyAsync() =>
        await js.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", _isDark ? "dark" : "light");
}
