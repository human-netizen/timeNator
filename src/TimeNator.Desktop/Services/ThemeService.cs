using Avalonia;
using Avalonia.Styling;
using TimeNator.Shared;

namespace TimeNator.Desktop.Services;

/// <summary>Applies the user's theme to the whole application. "system" follows Windows.</summary>
public class ThemeService
{
    public void Apply(string? themeKey)
    {
        if (Application.Current is not { } app)
            return;
        app.RequestedThemeVariant = themeKey switch
        {
            Themes.Light => ThemeVariant.Light,
            Themes.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
