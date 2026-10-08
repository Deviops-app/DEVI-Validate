using System.IO;
using DeviValidate.Core.Expected.Vendor;

namespace DeviValidate.Core.Expected;

public static class ExpectedHashReader
{
	public static ExpectedHashes ReadFile(string path, ExpectedReadOptions? options = null)
	{
		return ReadText(File.ReadAllText(path), Path.GetFileName(path), options);
	}

	public static ExpectedHashes ReadText(string content, string? fileName, ExpectedReadOptions? options = null)
	{
		content = StripBom(content);
		if (options == null)
		{
			options = new ExpectedReadOptions();
		}
		string text = (string.IsNullOrWhiteSpace(options.Format) ? "auto" : options.Format.Trim().ToLowerInvariant());
		if (text != null)
		{
			int length = text.Length;
			if (length != 3)
			{
				if (length != 4)
				{
					if (length == 8 && text == "manifest")
					{
						goto IL_0109;
					}
				}
				else
				{
					char c = text[0];
					if (c != 'a')
					{
						if (c == 'j' && text == "json")
						{
							goto IL_0109;
						}
					}
					else if (text == "auto")
					{
						return Detect(content, fileName, options);
					}
				}
			}
			else
			{
				switch (text[0])
				{
				case 'f':
					if (!(text == "ftk"))
					{
						break;
					}
					return FtkImagerLogParser.Parse(content, options.Algorithm);
				case 'c':
					if (!(text == "csv"))
					{
						break;
					}
					return DelimitedHashParser.Parse(content, ',', options);
				case 't':
					if (!(text == "tsv"))
					{
						break;
					}
					return DelimitedHashParser.Parse(content, '\t', options);
				case 's':
					if (!(text == "sum"))
					{
						break;
					}
					return SumFileParser.Parse(content, options.Algorithm);
				}
			}
		}
		throw new InvalidDataException("Format must be auto, sum, csv, tsv, manifest, or ftk.");
		IL_0109:
		return DeviManifestParser.Parse(content, options.Algorithm);
	}

	private static ExpectedHashes Detect(string content, string? fileName, ExpectedReadOptions options)
	{
		if (content.TrimStart().StartsWith('{'))
		{
			return DeviManifestParser.Parse(content, options.Algorithm);
		}
		if (FtkImagerLogParser.LooksLike(content))
		{
			return FtkImagerLogParser.Parse(content, options.Algorithm);
		}
		bool flag;
		switch ((fileName == null) ? "" : Path.GetExtension(fileName).ToLowerInvariant())
		{
		case ".csv":
			return DelimitedHashParser.Parse(content, ',', options);
		case ".tsv":
		case ".tab":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return DelimitedHashParser.Parse(content, '\t', options);
		}
		if (DelimitedHashParser.HasRecognizedHeader(content))
		{
			return DelimitedHashParser.Parse(content, DelimitedHashParser.GuessDelimiter(content), options);
		}
		foreach (IVendorReportParser item in VendorParserRegistry.All)
		{
			if (item.CanParse(content, fileName))
			{
				return item.Parse(content, options.Algorithm);
			}
		}
		return SumFileParser.Parse(content, options.Algorithm);
	}

	private static string StripBom(string content)
	{
		if (content.Length <= 0 || content[0] != '\ufeff')
		{
			return content;
		}
		return content.TrimStart('\ufeff');
	}
}
