using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected;

/// <summary>
/// Reads MD5, SHA-1, and SHA-256 checksum lines from an FTK Imager text log.
/// Those lines record the hash of the acquired data. They are not the hash of an E01 container.
/// </summary>
public static class FtkImagerLogParser
{
	public const string FormatName = "FTK Imager text log";

	public const string Note = "The expected hash is the acquired-data checksum from an FTK Imager text log. It can be compared with a raw image of that data. It is not the hash of an E01 or L01 container file.";

	private static readonly Regex ChecksumLine = new Regex("^\\s*(MD5|SHA1|SHA-1|SHA256|SHA-256)\\s+checksum:\\s*([0-9A-Fa-f]+)(?:\\s*:\\s*\\S+)?\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex ImagePathLine = new Regex("^\\s*Image Path:\\s*(.+?)\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static bool LooksLike(string content)
	{
		if (!content.Contains("checksum:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!content.Contains("FTK", StringComparison.OrdinalIgnoreCase) && !content.Contains("Imager", StringComparison.OrdinalIgnoreCase) && !content.Contains("Image Verification", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return content.Split('\n').Any((string line) => ChecksumLine.IsMatch(line.TrimEnd('\r')));
	}

	public static ExpectedHashes Parse(string content, HashAlgorithmKind? requested)
	{
		if (!LooksLike(content))
		{
			throw new InvalidDataException("The file is not an FTK Imager text log with checksum lines.");
		}
		string text = null;
		bool flag = false;
		int num = 0;
		List<ExpectedHashEntry> list = new List<ExpectedHashEntry>();
		string[] array = content.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].TrimEnd('\r');
			Match match = ImagePathLine.Match(text2);
			if (match.Success)
			{
				if (text == null)
				{
					text = match.Groups[1].Value.Trim();
				}
				num = Math.Max(num, 1);
				flag = false;
				continue;
			}
			if (text2.Trim().Equals("Segment list:", StringComparison.OrdinalIgnoreCase) || text2.Contains("Segment list:", StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
				continue;
			}
			if (flag)
			{
				string text3 = text2.Trim();
				if (text3.Length == 0)
				{
					flag = false;
					continue;
				}
				if (text3.Contains('\\') || text3.Contains('/') || (text3.Length >= 3 && char.IsLetter(text3[0]) && text3[1] == ':' && (text3[2] == '\\' || text3[2] == '/')))
				{
					if (text == null)
					{
						text = text3;
					}
					num++;
					continue;
				}
				flag = false;
			}
			Match match2 = ChecksumLine.Match(text2);
			if (match2.Success && HashAlgorithms.TryParse(match2.Groups[1].Value, out var kind))
			{
				string text4 = HashAlgorithms.NormalizeHex(match2.Groups[2].Value);
				if (text4.Length != HashAlgorithms.HexLength(kind))
				{
					throw new InvalidDataException(HashAlgorithms.DisplayName(kind) + " checksum in the FTK Imager log is not the expected length.");
				}
				list.Add(new ExpectedHashEntry
				{
					Path = ((text == null) ? "" : PathKeys.Normalize(text)),
					Hash = text4,
					Algorithm = kind
				});
			}
		}
		if (num > 1)
		{
			throw new InvalidDataException("This FTK Imager log lists more than one image segment. This version compares the log hash with a single raw image file, not a split set.");
		}
		return ExpectedHashSelection.Prefer("FTK Imager text log", "The expected hash is the acquired-data checksum from an FTK Imager text log. It can be compared with a raw image of that data. It is not the hash of an E01 or L01 container file.", list, requested);
	}
}
