using System;
using System.IO;
using PdfSharp.Fonts;

namespace DeviValidate.Core.Reporting;

internal static class PdfFonts
{
	private sealed class Resolver : IFontResolver, IFontResolverMarker
	{
		public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
		{
			return new FontResolverInfo((!familyName.Contains("Mono", StringComparison.OrdinalIgnoreCase)) ? (isBold ? "sans-bold" : "sans") : (isBold ? "mono-bold" : "mono"));
		}

		public byte[] GetFont(string faceName)
		{
			return File.ReadAllBytes(faceName switch
			{
				"sans-bold" => First("/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf", "C:\\Windows\\Fonts\\segoeuib.ttf", "C:\\Windows\\Fonts\\arialbd.ttf"), 
				"mono-bold" => First("/usr/share/fonts/truetype/liberation/LiberationMono-Bold.ttf", "C:\\Windows\\Fonts\\consolab.ttf", "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf"), 
				"mono" => First("/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf", "C:\\Windows\\Fonts\\consola.ttf", "C:\\Windows\\Fonts\\cour.ttf"), 
				_ => First("/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf", "C:\\Windows\\Fonts\\segoeui.ttf", "C:\\Windows\\Fonts\\arial.ttf"), 
			});
		}

		private static string First(params string[] paths)
		{
			foreach (string text in paths)
			{
				if (File.Exists(text))
				{
					return text;
				}
			}
			throw new InvalidOperationException("DEVI Validate could not find a font for the PDF record. Install Liberation Sans and Liberation Mono, or run on Windows.");
		}
	}

	public const string Sans = "Devi Sans";

	public const string Mono = "Devi Mono";

	private static readonly object Gate = new object();

	private static bool _ready;

	public static void Ensure()
	{
		if (_ready)
		{
			return;
		}
		lock (Gate)
		{
			if (!_ready)
			{
				GlobalFontSettings.FontResolver = new Resolver();
				_ready = true;
			}
		}
	}
}
