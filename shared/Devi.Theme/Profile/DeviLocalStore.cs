using System;
using System.IO;
using System.Text.Json;

namespace Devi.Theme.Profile;

/// <summary>
/// Per-user settings files for the DEVI Windows apps. Everything lives under
/// %LOCALAPPDATA%\DEVI on this computer. Nothing here uses the network or stores evidence data.
/// </summary>
public static class DeviLocalStore
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>%LOCALAPPDATA%\DEVI. Tests and tools may point it elsewhere.</summary>
    public static string Root { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DEVI");

    /// <summary>The shared examiner profile every DEVI app reads and writes.</summary>
    public static string ProfilePath => Path.Combine(Root, "profile.json");

    /// <summary>One app's own settings, for example %LOCALAPPDATA%\DEVI\Validate\settings.json.</summary>
    public static string AppSettingsPath(string app) => Path.Combine(Root, app, "settings.json");

    public static T Load<T>(string path) where T : class, new()
    {
        try
        {
            if (!File.Exists(path))
            {
                return new T();
            }
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? new T();
        }
        catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException || ex is NotSupportedException)
        {
            return new T();
        }
    }

    public static void Save<T>(string path, T value)
    {
        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, Options) + Environment.NewLine);
        File.Move(temp, path, overwrite: true);
    }

    public static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
