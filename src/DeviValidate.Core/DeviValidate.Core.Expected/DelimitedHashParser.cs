using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected;

/// <summary>Reads a CSV or TSV that has a path column and a hash column.</summary>
public static class DelimitedHashParser
{
	private static readonly string[] PathHeaders = new string[8] { "path", "relativepath", "file", "filename", "name", "fullpath", "evidence", "filepath" };

	private static readonly string[] HashHeaders = new string[7] { "sha256", "sha1", "md5", "hash", "checksum", "digest", "value" };

	public static bool HasRecognizedHeader(string content)
	{
		char delimiter = GuessDelimiter(content);
		List<List<string>> list = ParseRows(content, delimiter);
		if (list.Count == 0)
		{
			return false;
		}
		if (FindColumn(list[0], PathHeaders) >= 0)
		{
			return FindHashColumn(list[0], null) >= 0;
		}
		return false;
	}

	public static char GuessDelimiter(string content)
	{
		string source = FirstLine(content);
		int num = source.Count((char c) => c == '\t');
		int num2 = source.Count((char c) => c == ',');
		if (num <= num2)
		{
			return ',';
		}
		return '\t';
	}

	public static ExpectedHashes Parse(string content, char delimiter, ExpectedReadOptions options)
	{
		List<List<string>> list = ParseRows(content, delimiter);
		if (list.Count == 0)
		{
			throw new InvalidDataException("The delimited file is empty.");
		}
		List<string> list2 = list[0];
		int num = 0;
		HashAlgorithmKind? hashAlgorithmKind = null;
		int num2;
		int num3;
		if (!string.IsNullOrWhiteSpace(options.PathColumn) || !string.IsNullOrWhiteSpace(options.HashColumn))
		{
			if (string.IsNullOrWhiteSpace(options.PathColumn) || string.IsNullOrWhiteSpace(options.HashColumn))
			{
				throw new InvalidDataException("Pass both --path-column and --hash-column.");
			}
			if (IsIndex(options.PathColumn) && IsIndex(options.HashColumn) && !RowLooksLikeHeader(list2))
			{
				num2 = ResolveIndex(options.PathColumn, list2.Count);
				num3 = ResolveIndex(options.HashColumn, list2.Count);
				num = 0;
			}
			else
			{
				num2 = ResolveColumn(list2, options.PathColumn);
				num3 = ResolveColumn(list2, options.HashColumn);
				num = 1;
				hashAlgorithmKind = AlgorithmFromHeader(list2[num3]);
			}
		}
		else
		{
			if (!RowLooksLikeHeader(list2))
			{
				throw HeaderError(list2);
			}
			num2 = FindColumn(list2, PathHeaders);
			num3 = FindHashColumn(list2, options.Algorithm);
			if (num2 < 0 || num3 < 0)
			{
				throw HeaderError(list2);
			}
			hashAlgorithmKind = AlgorithmFromHeader(list2[num3]);
			num = 1;
		}
		if (num2 == num3)
		{
			throw new InvalidDataException("The path column and the hash column must be different.");
		}
		List<ExpectedHashEntry> list3 = new List<ExpectedHashEntry>();
		for (int i = num; i < list.Count; i++)
		{
			List<string> list4 = list[i];
			if (list4.All((string field) => string.IsNullOrWhiteSpace(field)))
			{
				continue;
			}
			if (num2 >= list4.Count || num3 >= list4.Count)
			{
				throw new InvalidDataException($"Row {i + 1} does not have the path and hash columns.");
			}
			string text = list4[num2].Trim();
			string text2 = list4[num3].Trim();
			if (text.Length == 0 && text2.Length == 0)
			{
				continue;
			}
			string text3 = HashAlgorithms.NormalizeHex(text2);
			if (!HashAlgorithms.TryFromHexLength(text3.Length, out var kind))
			{
				throw new InvalidDataException($"Row {i + 1} hash is not MD5, SHA-1, or SHA-256.");
			}
			if (hashAlgorithmKind.HasValue)
			{
				HashAlgorithmKind valueOrDefault = hashAlgorithmKind.GetValueOrDefault();
				if (valueOrDefault != kind)
				{
					throw new InvalidDataException($"Row {i + 1} is {HashAlgorithms.DisplayName(kind)}, and the column is {HashAlgorithms.DisplayName(valueOrDefault)}.");
				}
			}
			long? size = null;
			int num4 = FindColumn(list2, new string[3] { "size", "bytes", "length" });
			if (num == 1 && num4 >= 0 && num4 < list4.Count && long.TryParse(list4[num4].Trim(), out var result))
			{
				size = result;
			}
			list3.Add(new ExpectedHashEntry
			{
				Path = PathKeys.Normalize(text),
				Hash = text3,
				Algorithm = kind,
				Size = size
			});
		}
		HashAlgorithmKind? requested = options.Algorithm ?? hashAlgorithmKind;
		return ExpectedHashSelection.Strict((delimiter == '\t') ? "TSV" : "CSV", null, list3, requested);
	}

	private static InvalidDataException HeaderError(IReadOnlyList<string> header)
	{
		string text = string.Join(", ", header.Select((string name, int index) => $"{index + 1}:{name}"));
		return new InvalidDataException("Could not find path and hash columns. Columns: " + text + ". Re-run with --path-column and --hash-column (a header name or a 1-based index).");
	}

	private static bool RowLooksLikeHeader(IReadOnlyList<string> row)
	{
		if (FindColumn(row, PathHeaders) < 0)
		{
			return FindHashColumn(row, null) >= 0;
		}
		return true;
	}

	private static int FindHashColumn(IReadOnlyList<string> header, HashAlgorithmKind? requested)
	{
		string text;
		string name2;
		int num2;
		switch (requested)
		{
		case HashAlgorithmKind.Sha256:
			text = "sha256";
			goto IL_0043;
		case HashAlgorithmKind.Sha1:
			text = "sha1";
			goto IL_0043;
		case HashAlgorithmKind.Md5:
			text = "md5";
			goto IL_0043;
		default:
			text = "";
			goto IL_0043;
		case null:
			{
				string[] array = new string[3] { "sha256", "sha1", "md5" };
				foreach (string name in array)
				{
					int num = IndexOfKey(header, name);
					if (num >= 0)
					{
						return num;
					}
				}
				break;
			}
			IL_0043:
			name2 = text;
			num2 = IndexOfKey(header, name2);
			if (num2 >= 0)
			{
				return num2;
			}
			break;
		}
		return FindColumn(header, HashHeaders);
	}

	private static HashAlgorithmKind? AlgorithmFromHeader(string header)
	{
		string text = Key(header);
		HashAlgorithmKind kind;
		bool flag = HashAlgorithms.TryParse(text, out kind);
		if (flag)
		{
			bool flag2;
			switch (text)
			{
			case "sha256":
			case "sha1":
			case "md5":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (flag)
		{
			return kind;
		}
		return null;
	}

	private static int FindColumn(IReadOnlyList<string> header, IReadOnlyList<string> names)
	{
		foreach (string name in names)
		{
			int num = IndexOfKey(header, name);
			if (num >= 0)
			{
				return num;
			}
		}
		return -1;
	}

	private static int IndexOfKey(IReadOnlyList<string> header, string name)
	{
		for (int i = 0; i < header.Count; i++)
		{
			if (Key(header[i]) == name)
			{
				return i;
			}
		}
		return -1;
	}

	private static int ResolveColumn(IReadOnlyList<string> header, string spec)
	{
		if (IsIndex(spec))
		{
			return ResolveIndex(spec, header.Count);
		}
		string text = Key(spec);
		int num = -1;
		for (int i = 0; i < header.Count; i++)
		{
			if (!(Key(header[i]) != text))
			{
				if (num >= 0)
				{
					throw new InvalidDataException("Column '" + spec + "' matches more than one header.");
				}
				num = i;
			}
		}
		if (num < 0)
		{
			throw new InvalidDataException("Column '" + spec + "' was not found. Headers: " + string.Join(", ", header));
		}
		return num;
	}

	private static int ResolveIndex(string spec, int count)
	{
		if (!int.TryParse(spec, out var result) || result < 1 || result > count)
		{
			throw new InvalidDataException($"Column {spec} is outside the range 1 to {count}.");
		}
		return result - 1;
	}

	private static bool IsIndex(string? spec)
	{
		if (int.TryParse(spec, out var result))
		{
			return result > 0;
		}
		return false;
	}

	private static string Key(string value)
	{
		return new string((from c in value.Trim().ToLowerInvariant()
			where c != ' ' && c != '_' && c != '-'
			select c).ToArray());
	}

	private static string FirstLine(string content)
	{
		int num = content.IndexOf('\n');
		if (num >= 0)
		{
			return content.Substring(0, num);
		}
		return content;
	}

	internal static List<List<string>> ParseRows(string content, char delimiter)
	{
		List<List<string>> list = new List<List<string>>();
		List<string> list2 = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < content.Length; i++)
		{
			char c = content[i];
			if (flag)
			{
				if (c == '"')
				{
					if (i + 1 < content.Length && content[i + 1] == '"')
					{
						stringBuilder.Append('"');
						i++;
					}
					else
					{
						flag = false;
					}
				}
				else
				{
					stringBuilder.Append(c);
				}
				continue;
			}
			if (c == '"')
			{
				flag = true;
				continue;
			}
			if (c == delimiter)
			{
				list2.Add(stringBuilder.ToString());
				stringBuilder.Clear();
				continue;
			}
			switch (c)
			{
			case '\n':
				list2.Add(stringBuilder.ToString());
				stringBuilder.Clear();
				if (list2.Any((string item) => item.Length > 0))
				{
					list.Add(list2);
				}
				list2 = new List<string>();
				break;
			default:
				stringBuilder.Append(c);
				break;
			case '\r':
				break;
			}
		}
		if (stringBuilder.Length > 0 || list2.Count > 0)
		{
			list2.Add(stringBuilder.ToString());
			if (list2.Any((string item) => item.Length > 0))
			{
				list.Add(list2);
			}
		}
		return list;
	}
}
