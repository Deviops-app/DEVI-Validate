using System;
using System.Collections.Generic;
using System.IO;

namespace DeviValidate.Core.IO;

/// <summary>
/// Refuses to write a report inside the evidence location.
/// For a file, the evidence location is the folder that contains that file.
/// </summary>
public static class OutputPathGuard
{
	public const string RefusalTitle = "Save the report outside the evidence";

	public const string Refusal = "Reports must be saved outside the evidence location. A report file saved inside that folder is added to the set and changes the folder hash. Choose another folder, such as Documents\\DEVI Validate.";

	/// <summary>
	/// A folder that is not inside the evidence location.
	/// Prefers the last folder that accepted a report, then Documents\DEVI Validate, then the desktop.
	/// </summary>
	public static string SuggestOutputDirectory(IReadOnlyList<string>? evidencePaths, string? lastGoodDirectory)
	{
		List<string> paths = new List<string>();
		if (evidencePaths != null)
		{
			foreach (string path in evidencePaths)
			{
				if (!string.IsNullOrWhiteSpace(path))
				{
					paths.Add(path);
				}
			}
		}
		if (paths.Count <= 1)
		{
			return SuggestOutputDirectory(paths.Count == 0 ? null : paths[0], lastGoodDirectory);
		}
		foreach (string item in Candidates(lastGoodDirectory))
		{
			if (string.IsNullOrWhiteSpace(item))
			{
				continue;
			}
			string fullPath;
			try
			{
				fullPath = Path.GetFullPath(item);
			}
			catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
			{
				continue;
			}
			bool blocked = false;
			foreach (string path in paths)
			{
				if (Blocks(path, fullPath))
				{
					blocked = true;
					break;
				}
			}
			if (!blocked)
			{
				return fullPath;
			}
		}
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrWhiteSpace(folderPath))
		{
			return folderPath;
		}
		return Path.GetTempPath();
	}

	public static string SuggestOutputDirectory(string? evidencePath, string? lastGoodDirectory)
	{
		foreach (string item in Candidates(lastGoodDirectory))
		{
			if (!string.IsNullOrWhiteSpace(item))
			{
				string fullPath;
				try
				{
					fullPath = Path.GetFullPath(item);
				}
				catch (Exception ex) when (((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? 1 : 0) != 0)
				{
					continue;
				}
				if (string.IsNullOrWhiteSpace(evidencePath) || !Blocks(evidencePath, fullPath))
				{
					return fullPath;
				}
			}
		}
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrWhiteSpace(folderPath))
		{
			return folderPath;
		}
		return Path.GetTempPath();
	}

	private static IEnumerable<string> Candidates(string? lastGoodDirectory)
	{
		if (!string.IsNullOrWhiteSpace(lastGoodDirectory))
		{
			yield return lastGoodDirectory;
		}
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		if (!string.IsNullOrWhiteSpace(folderPath))
		{
			yield return Path.Combine(folderPath, "DEVI Validate");
		}
		string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
		if (!string.IsNullOrWhiteSpace(folderPath2))
		{
			yield return folderPath2;
		}
	}

	private static bool Blocks(string evidencePath, string outputPath)
	{
		try
		{
			EnsureOutside(evidencePath, outputPath);
			return false;
		}
		catch (InvalidOperationException)
		{
			return true;
		}
		catch (ArgumentException)
		{
			return true;
		}
	}

	public static void EnsureOutside(string evidencePath, string outputPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(evidencePath, "evidencePath");
		ArgumentException.ThrowIfNullOrWhiteSpace(outputPath, "outputPath");
		string fullPath = Path.GetFullPath(evidencePath);
		string root = ((File.Exists(fullPath) && !Directory.Exists(fullPath)) ? (Path.GetDirectoryName(ResolveFinal(fullPath)) ?? fullPath) : ResolveFinal(fullPath));
		string candidate = ResolveFinal(outputPath);
		if (IsInside(root, candidate))
		{
			throw new InvalidOperationException("Reports must be saved outside the evidence location. A report file saved inside that folder is added to the set and changes the folder hash. Choose another folder, such as Documents\\DEVI Validate.");
		}
	}

	public static bool IsInside(string root, string candidate)
	{
		StringComparison comparisonType = (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
		candidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
		if (string.Equals(root, candidate, comparisonType))
		{
			return true;
		}
		string value = root + Path.DirectorySeparatorChar;
		return candidate.StartsWith(value, comparisonType);
	}

	/// <summary>Resolves existing symbolic links, including a link that is a parent of a path that does not exist yet.</summary>
	public static string ResolveFinal(string path)
	{
		string fullPath = Path.GetFullPath(path);
		string text = fullPath;
		string text2 = null;
		while (!string.IsNullOrEmpty(text) && !File.Exists(text) && !Directory.Exists(text))
		{
			string directoryName = Path.GetDirectoryName(text);
			if (string.IsNullOrEmpty(directoryName) || directoryName == text)
			{
				return fullPath;
			}
			string fileName = Path.GetFileName(text);
			text2 = ((text2 == null) ? fileName : Path.Combine(fileName, text2));
			text = directoryName;
		}
		if (string.IsNullOrEmpty(text))
		{
			return fullPath;
		}
		FileSystemInfo fileSystemInfo = ((Directory.Exists(text) && !File.Exists(text)) ? ((FileSystemInfo)new DirectoryInfo(text)) : ((FileSystemInfo)new FileInfo(text)));
		string text3 = fileSystemInfo.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? fileSystemInfo.FullName;
		if (text2 != null)
		{
			return Path.GetFullPath(Path.Combine(text3, text2));
		}
		return text3;
	}
}
