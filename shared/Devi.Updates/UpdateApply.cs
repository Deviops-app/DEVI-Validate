using System.Diagnostics;
using System.Globalization;

namespace Devi.Updates;

/// <summary>Starts an installer or unpacks a portable zip after the current process exits.</summary>
public static class UpdateApply
{
	public static void StartInstaller(string path)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = path,
			UseShellExecute = true
		});
	}

	public static void ShowFile(string path)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = "explorer.exe",
			Arguments = "/select,\"" + path + "\"",
			UseShellExecute = true
		});
	}

	/// <param name="folderPrefix">Folder name prefix such as DEVI-Decrypt.</param>
	/// <param name="localUpdatesRoot">Fallback under LocalAppData when the parent of the running folder is missing.</param>
	public static string PlanPortableDirectory(string folderPrefix, string version, string localUpdatesRoot)
	{
		string text = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string? parent = Directory.GetParent(text)?.FullName;
		string folderName = folderPrefix + "-" + Sanitize(version);
		string root = string.IsNullOrEmpty(parent) ? localUpdatesRoot : parent;
		string candidate = Path.Combine(root, folderName);
		int n = 2;
		while (Directory.Exists(candidate) || File.Exists(candidate) || IsInside(candidate, text))
		{
			candidate = Path.Combine(root, folderName + "_" + n.ToString(CultureInfo.InvariantCulture));
			n++;
			if (n > 50)
			{
				throw new IOException("Could not choose a folder for the new copy.");
			}
		}

		return candidate;
	}

	public static void ExtractAfterExit(string zipPath, string destination, string scriptNamePrefix)
	{
		string script = Path.Combine(Path.GetTempPath(), scriptNamePrefix + "-extract-" + Guid.NewGuid().ToString("N") + ".ps1");
		string body = string.Join("\r\n",
			"$ErrorActionPreference = 'Stop'",
			"Wait-Process -Id " + Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + " -ErrorAction SilentlyContinue",
			"New-Item -ItemType Directory -Force -Path " + Quote(destination) + " | Out-Null",
			"Expand-Archive -LiteralPath " + Quote(zipPath) + " -DestinationPath " + Quote(destination) + " -Force",
			"Start-Process explorer.exe " + Quote(destination));
		File.WriteAllText(script, body + "\r\n");
		Process.Start(new ProcessStartInfo
		{
			FileName = "powershell.exe",
			Arguments = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(script),
			UseShellExecute = false,
			CreateNoWindow = true
		});
	}

	private static bool IsInside(string path, string parent)
	{
		string fullPath = Path.GetFullPath(path);
		string fullParent = Path.GetFullPath(parent);
		if (!string.Equals(fullPath, fullParent, StringComparison.OrdinalIgnoreCase))
		{
			return fullPath.StartsWith(fullParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
		}

		return true;
	}

	private static string Sanitize(string version)
	{
		char[] ok = version.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '.').ToArray();
		return ok.Length != 0 ? new string(ok) : "release";
	}

	private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
