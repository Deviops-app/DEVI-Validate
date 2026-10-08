using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected;

/// <summary>Reads GNU sha256sum/md5sum lines, BSD sum lines, and a file that is only a hash.</summary>
public static class SumFileParser
{
	public const string GnuName = "GNU sum";

	public const string BsdName = "BSD sum";

	public const string BareName = "bare hash";

	private static readonly Regex BsdPattern = new Regex("^(MD5|SHA1|SHA-1|SHA256|SHA-256) \\((.*)\\) = ([0-9A-Fa-f]+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex LabeledPattern = new Regex("^(MD5|SHA1|SHA-1|SHA256|SHA-256)\\s*[:=]\\s*([0-9A-Fa-f]+)\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static ExpectedHashes Parse(string content, HashAlgorithmKind? requested)
	{
		List<ExpectedHashEntry> list = new List<ExpectedHashEntry>();
		string text = null;
		string text2 = null;
		foreach (string item in ContentLines(content))
		{
			if (TryBsd(item, out HashAlgorithmKind algorithm, out string path, out string hash))
			{
				if (text != null && text != "BSD sum")
				{
					throw Mixed();
				}
				text = "BSD sum";
				list.Add(Entry(path, hash, algorithm));
			}
			else if (TryGnu(item, out hash, out path))
			{
				if (text != null && text != "GNU sum")
				{
					throw Mixed();
				}
				text = "GNU sum";
				list.Add(Entry(path, hash, HashAlgorithms.FromHexLength(hash)));
			}
			else if (text2 == null)
			{
				text2 = item;
			}
		}
		if (list.Count == 0 && text2 != null && TryBare(content, out string hash2, out HashAlgorithmKind algorithm2))
		{
			list.Add(Entry("", hash2, algorithm2));
			text = "bare hash";
			text2 = null;
		}
		if (list.Count == 0 && TryBare(content, out hash2, out algorithm2))
		{
			list.Add(Entry("", hash2, algorithm2));
			text = "bare hash";
		}
		if (text2 != null)
		{
			throw new InvalidDataException("Could not read this hash line: " + Truncate(text2));
		}
		return ExpectedHashSelection.Strict(text ?? "GNU sum", null, list, requested);
	}

	private static InvalidDataException Mixed()
	{
		return new InvalidDataException("The expected file mixes hash formats.");
	}

	internal static IEnumerable<string> ContentLines(string content)
	{
		using StringReader reader = new StringReader(content);
		while (true)
		{
			string text = reader.ReadLine();
			if (text != null)
			{
				string text2 = text.Trim().Trim('\ufeff').Trim();
				if (text2.Length != 0 && !text2.StartsWith('#'))
				{
					yield return text2;
				}
				continue;
			}
			break;
		}
	}

	private static bool TryGnu(string line, out string hash, out string path)
	{
		hash = "";
		path = "";
		int i;
		for (i = 0; i < line.Length && HashAlgorithms.IsHexChar(line[i]); i++)
		{
		}
		if (!HashAlgorithms.TryFromHexLength(i, out var _))
		{
			return false;
		}
		if (i >= line.Length || line[i] != ' ')
		{
			return false;
		}
		string text = line;
		int num = i + 1;
		string text2 = text.Substring(num, text.Length - num);
		if (text2.StartsWith('*'))
		{
			text = text2;
			text2 = text.Substring(1, text.Length - 1);
		}
		else if (text2.StartsWith(' '))
		{
			text = text2;
			text2 = text.Substring(1, text.Length - 1);
		}
		if (text2.Length == 0)
		{
			return false;
		}
		hash = line.Substring(0, i);
		path = text2;
		return true;
	}

	private static bool TryBsd(string line, out HashAlgorithmKind algorithm, out string path, out string hash)
	{
		algorithm = HashAlgorithmKind.Sha256;
		path = "";
		hash = "";
		Match match = BsdPattern.Match(line);
		if (!match.Success)
		{
			return false;
		}
		if (!HashAlgorithms.TryParse(match.Groups[1].Value, out algorithm))
		{
			return false;
		}
		path = match.Groups[2].Value;
		hash = match.Groups[3].Value;
		return HashAlgorithms.HexLength(algorithm) == hash.Length;
	}

	private static bool TryBare(string content, out string hash, out HashAlgorithmKind algorithm)
	{
		hash = "";
		algorithm = HashAlgorithmKind.Sha256;
		string text = content.Trim();
		Match match = LabeledPattern.Match(text);
		if (match.Success && HashAlgorithms.TryParse(match.Groups[1].Value, out algorithm))
		{
			hash = match.Groups[2].Value;
			return HashAlgorithms.HexLength(algorithm) == hash.Length;
		}
		if (text.Contains('\n') || text.Contains(' ') || text.Contains('\t'))
		{
			return false;
		}
		if (!HashAlgorithms.TryFromHexLength(text.Length, out algorithm) || !HashAlgorithms.IsHex(text))
		{
			return false;
		}
		hash = text;
		return true;
	}

	private static ExpectedHashEntry Entry(string path, string hash, HashAlgorithmKind algorithm)
	{
		string text = HashAlgorithms.NormalizeHex(hash);
		if (text.Length != HashAlgorithms.HexLength(algorithm))
		{
			throw new InvalidDataException($"Hash length does not match {HashAlgorithms.DisplayName(algorithm)} for '{path}'.");
		}
		return new ExpectedHashEntry
		{
			Path = ((path.Length == 0) ? "" : PathKeys.Normalize(path)),
			Hash = text,
			Algorithm = algorithm
		};
	}

	private static string Truncate(string line)
	{
		if (line.Length > 120)
		{
			return line.Substring(0, 120) + "...";
		}
		return line;
	}
}
