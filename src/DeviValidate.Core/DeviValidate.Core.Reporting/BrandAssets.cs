using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace DeviValidate.Core.Reporting;

/// <summary>Official DEVI marks. SVG path data is copied from the brand files. Only the color changes for print.</summary>
public static class BrandAssets
{
	private const string WordmarkSvgName = "DeviValidate.Core.Reporting.Brand.devi-wordmark-white.svg";

	private const string WordmarkPngName = "DeviValidate.Core.Reporting.Brand.devi-wordmark-dark.png";

	private const string MarkPngName = "DeviValidate.Core.Reporting.Brand.devi-d-dark.png";

	public static string WordmarkDarkSvg { get; } = Recolor(ReadText("DeviValidate.Core.Reporting.Brand.devi-wordmark-white.svg"));


	public static byte[] WordmarkDarkPng { get; } = ReadBytes("DeviValidate.Core.Reporting.Brand.devi-wordmark-dark.png");


	public static byte[] MarkDarkPng { get; } = ReadBytes("DeviValidate.Core.Reporting.Brand.devi-d-dark.png");


	/// <summary>White wordmark for the dark header band on every record page.</summary>
	public static byte[] WordmarkWhitePng { get; } = ReadBytes("DeviValidate.Core.Reporting.Brand.devi-wordmark-white.png");


	/// <summary>Compact white wordmark for the header band on continuation pages.</summary>
	public static byte[] WordmarkCompactWhitePng { get; } = ReadBytes("DeviValidate.Core.Reporting.Brand.devi-wordmark-compact-white.png");


	/// <summary>White wordmark SVG for the dark header band of the HTML record.</summary>
	public static string WordmarkWhiteSvg { get; } = ReadText("DeviValidate.Core.Reporting.Brand.devi-wordmark-white.svg").Trim();


	private static string Recolor(string svg)
	{
		string text = svg.Replace("#FFFFFF", "#141416", StringComparison.Ordinal);
		if (text.Contains("#FFFFFF", StringComparison.Ordinal) || string.Equals(text, svg, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("The DEVI wordmark could not be recolored for a light page.");
		}
		return text.Trim();
	}

	private static string ReadText(string name)
	{
		return Encoding.UTF8.GetString(ReadBytes(name));
	}

	private static byte[] ReadBytes(string name)
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new InvalidOperationException("Missing brand resource " + name);
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}
