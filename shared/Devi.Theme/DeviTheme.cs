using System;
using System.Windows;
using System.Windows.Media;

namespace Devi.Theme;

/// <summary>
/// Code access to the theme tokens. App code asks for a token by name and never builds a
/// color itself, so the palette stays in Themes/Tokens.xaml.
/// </summary>
public static class DeviTheme
{
    /// <summary>Pack URI an app merges into Application.Resources.</summary>
    public const string ResourceUri = "pack://application:,,,/Devi.Theme;component/Themes/Devi.xaml";

    private static ResourceDictionary? _fallback;

    public static Brush Brush(string key) => (Brush)Find(key);

    public static Color Color(string key) => (Color)Find(key);

    private static object Find(string key)
    {
        var app = Application.Current;
        if (app is not null && app.TryFindResource(key) is { } found)
        {
            return found;
        }

        _fallback ??= new ResourceDictionary { Source = new Uri(ResourceUri, UriKind.Absolute) };
        return _fallback[key] ?? throw new InvalidOperationException("The DEVI theme has no resource named " + key + ".");
    }
}
