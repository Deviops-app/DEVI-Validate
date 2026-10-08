using System;
using System.IO;

namespace DeviValidate.Core.Expected;

public static class ExpectedHashRules
{
	public static void EnsureCompatible(string evidencePath, ExpectedHashes expected)
	{
		if (!string.Equals(expected.FormatName, "FTK Imager text log", StringComparison.Ordinal))
		{
			if (expected.Entries.Count == 1 && expected.Entries[0].Path.Length == 0 && Directory.Exists(evidencePath))
			{
				throw new InvalidDataException("The expected file contains a single hash and no path. Point the tool at one evidence file, or use a list that includes paths.");
			}
			return;
		}
		if (Directory.Exists(evidencePath))
		{
			throw new InvalidOperationException("This FTK Imager log contains one acquired-data checksum, not a per-file list. Choose the single raw image file.");
		}
		if (!IsContainer(Path.GetFileName(evidencePath)))
		{
			return;
		}
		throw new InvalidOperationException("This FTK Imager log records the hash of the acquired data, not the hash of an E01 or L01 container. Choose a raw image, or provide a hash of the container file itself. Reading hashes stored inside E01 files is not available in this version.");
	}

	private static bool IsContainer(string name)
	{
		string extension = Path.GetExtension(name);
		if (extension.Equals(".e01", StringComparison.OrdinalIgnoreCase) || extension.Equals(".l01", StringComparison.OrdinalIgnoreCase) || extension.Equals(".ex01", StringComparison.OrdinalIgnoreCase) || extension.Equals(".lx01", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		bool flag = extension.Length == 4;
		if (flag)
		{
			bool flag2;
			switch (extension[1])
			{
			case 'E':
			case 'L':
			case 'e':
			case 'l':
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (flag && char.IsDigit(extension[2]) && char.IsDigit(extension[3]))
		{
			return true;
		}
		return false;
	}
}
