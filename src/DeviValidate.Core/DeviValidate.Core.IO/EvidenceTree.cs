using System;
using System.Collections.Generic;
using System.IO;

namespace DeviValidate.Core.IO;

/// <summary>Lists files under an evidence path. Reparse points are recorded and not followed.</summary>
public static class EvidenceTree
{
	public static EvidenceScan Scan(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path, "path");
		string fullPath = Path.GetFullPath(path);
		if (!ExistsOrLink(fullPath))
		{
			throw new DirectoryNotFoundException("Evidence path was not found: " + path);
		}
		if (IsReparsePoint(fullPath))
		{
			throw new InvalidOperationException("The evidence path is a symbolic link. DEVI Validate does not follow symbolic links. Pass the target path if you intend to read it.");
		}
		if (File.Exists(fullPath))
		{
			FileInfo fileInfo = new FileInfo(fullPath);
			EvidenceFile evidenceFile = new EvidenceFile(fileInfo.FullName, fileInfo.Name, fileInfo.Length);
			return new EvidenceScan(fullPath, new EvidenceFile[1] { evidenceFile }, Array.Empty<SkippedEntry>());
		}
		if (!Directory.Exists(fullPath))
		{
			throw new DirectoryNotFoundException("Evidence path was not found: " + path);
		}
		List<EvidenceFile> list = new List<EvidenceFile>();
		List<SkippedEntry> list2 = new List<SkippedEntry>();
		Walk(fullPath, fullPath, list, list2);
		list.Sort((EvidenceFile a, EvidenceFile b) => string.CompareOrdinal(a.RelativePath, b.RelativePath));
		list2.Sort((SkippedEntry a, SkippedEntry b) => string.CompareOrdinal(a.Path, b.Path));
		return new EvidenceScan(fullPath, list, list2);
	}

	private static void Walk(string root, string current, List<EvidenceFile> files, List<SkippedEntry> skipped)
	{
		IEnumerable<string> enumerable;
		try
		{
			enumerable = Directory.EnumerateFileSystemEntries(current);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			throw new IOException("Could not read directory '" + Relative(root, current) + "': " + ex.Message, ex);
		}
		foreach (string item in enumerable)
		{
			if (IsReparsePoint(item))
			{
				skipped.Add(new SkippedEntry
				{
					Path = Relative(root, item),
					Reason = "symbolic link"
				});
			}
			else if (Directory.Exists(item))
			{
				Walk(root, item, files, skipped);
			}
			else if (File.Exists(item))
			{
				FileInfo fileInfo = new FileInfo(item);
				files.Add(new EvidenceFile(fileInfo.FullName, Relative(root, item), fileInfo.Length));
			}
		}
	}

	private static string Relative(string root, string full)
	{
		return Path.GetRelativePath(root, full).Replace('\\', '/');
	}

	private static bool ExistsOrLink(string path)
	{
		try
		{
			File.GetAttributes(path);
			return true;
		}
		catch (Exception ex) when (((ex is FileNotFoundException || ex is DirectoryNotFoundException) ? 1 : 0) != 0)
		{
			return false;
		}
	}

	private static bool IsReparsePoint(string path)
	{
		return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
	}
}
