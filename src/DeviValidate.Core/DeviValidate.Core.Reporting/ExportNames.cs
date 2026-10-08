using System;
using System.Globalization;
using System.IO;
using System.Linq;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Reporting;

/// <summary>File names for exported records. Existing files are left in place.</summary>
public static class ExportNames
{
	public static string Stem(VerificationRecord record)
	{
		if (string.Equals(record.EvidenceKind, "folder", StringComparison.Ordinal))
		{
			if (string.IsNullOrWhiteSpace(record.SetHash))
			{
				throw new InvalidOperationException("This folder record has no set hash to use as a file name.");
			}
			return record.SetHash.ToLowerInvariant();
		}
		string? text = record.Files.Select((FileVerificationEntry file) => file.ComputedHash).FirstOrDefault((string value) => !string.IsNullOrWhiteSpace(value));
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("This file record has no computed hash to use as a file name.");
		}
		return HashAlgorithms.NormalizeHex(text);
	}

	public static string SelfTestStem(string version, DateTimeOffset when)
	{
		string text = new string(version.Select(delegate(char character)
		{
			bool flag = char.IsLetterOrDigit(character);
			if (!flag)
			{
				bool flag2 = ((character == '-' || character == '.') ? true : false);
				flag = flag2;
			}
			return (!flag) ? '_' : character;
		}).ToArray());
		string text2 = when.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		return "selftest_" + text + "_" + text2;
	}

	/// <summary>
	/// Returns one path per extension, all sharing the same suffix.
	/// The first free name has no suffix. Later names use _2, _3, and so on.
	/// </summary>
	public static string[] UniqueSet(string directory, string stem, params string[] extensions)
	{
		string directory2 = directory;
		string stem2 = stem;
		if (extensions.Length == 0)
		{
			throw new ArgumentException("Pass at least one extension.", "extensions");
		}
		for (int i = 1; i < 10000; i++)
		{
			string suffix = ((i == 1) ? "" : ("_" + i.ToString(CultureInfo.InvariantCulture)));
			string[] array = extensions.Select((string extension) => Path.Combine(directory2, stem2 + suffix + extension)).ToArray();
			if (array.All((string path) => !File.Exists(path)))
			{
				return array;
			}
		}
		throw new IOException("Could not find an unused file name.");
	}

	public static string Unused(string path)
	{
		if (!File.Exists(path))
		{
			return path;
		}
		string text = Path.GetDirectoryName(path);
		if (string.IsNullOrEmpty(text))
		{
			text = ".";
		}
		return UniqueSet(text, Path.GetFileNameWithoutExtension(path), Path.GetExtension(path))[0];
	}
}
