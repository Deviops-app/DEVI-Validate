using System;
using System.IO;
using System.Text.Json;
using Devi.Theme.Profile;

namespace DeviValidate.Desktop;

/// <summary>
/// DEVI Validate's own settings, in %LOCALAPPDATA%\DEVI\Validate\settings.json. The examiner name and
/// agency live in the shared DEVI profile instead. No case number, evidence path, or hash is stored.
/// </summary>
public sealed class ValidateSettings
{
	public bool CheckOnLaunch { get; set; }

	public string? DefaultValidatedBy { get; set; }

	public string? DefaultLabProcedure { get; set; }

	public string? DefaultAlgorithm { get; set; }

	public static string FilePath => DeviLocalStore.AppSettingsPath("Validate");

	/// <summary>1.0.0 and 1.0.1 kept only the update choice, in roaming AppData.</summary>
	private static string LegacyUpdatePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DEVI", "Validate", "update-settings.json");

	public static ValidateSettings Load()
	{
		if (File.Exists(FilePath))
		{
			return DeviLocalStore.Load<ValidateSettings>(FilePath);
		}
		ValidateSettings settings = new ValidateSettings();
		try
		{
			if (File.Exists(LegacyUpdatePath))
			{
				using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(LegacyUpdatePath));
				if (doc.RootElement.TryGetProperty("checkOnLaunch", out JsonElement value) && value.ValueKind == JsonValueKind.True)
				{
					settings.CheckOnLaunch = true;
				}
			}
		}
		catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
		{
		}
		return settings;
	}

	public void Save()
	{
		DefaultValidatedBy = Clean(DefaultValidatedBy);
		DefaultLabProcedure = Clean(DefaultLabProcedure);
		DefaultAlgorithm = Clean(DefaultAlgorithm);
		DeviLocalStore.Save(FilePath, this);
	}

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
