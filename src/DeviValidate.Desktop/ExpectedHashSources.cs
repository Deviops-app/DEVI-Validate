using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeviValidate.Desktop;

/// <summary>
/// Choices for Lab procedure/reference: where the expected hash came from. Product names are
/// identification labels only and imply no affiliation with or endorsement by their owners.
/// The value is stored as plain text, exactly as shown, so free-text values from earlier
/// versions read the same way.
/// </summary>
public static class ExpectedHashSources
{
	public const string Other = "Other (type your own)";

	public const string FtkImagerExterro = "Expected hash from Exterro FTK Imager";

	public const string FtkImagerAccessData = "Expected hash from FTK Imager log (AccessData era)";

	public const string SumFile = "Sum file (sha256sum / md5sum)";

	public const string CsvList = "CSV hash list";

	/// <summary>Alphabetical by product, then the generic sources.</summary>
	public static IReadOnlyList<string> All { get; } = new[]
	{
		"Expected hash from Autopsy",
		"Expected hash from Belkasoft X Forensic",
		"Expected hash from Cellebrite Inseyets Physical Analyzer (Inseyets.PA)",
		"Expected hash from Cellebrite Inseyets UFED (Inseyets.UFED)",
		"Expected hash from dc3dd or dd log",
		"Expected hash from Exterro FTK (Forensic Toolkit)",
		FtkImagerExterro,
		FtkImagerAccessData,
		"Expected hash from Guymager",
		"Expected hash from Magnet Axiom",
		"Expected hash from Magnet Graykey",
		"Expected hash from MSAB XRY",
		"Expected hash from OpenText Forensic (formerly EnCase)",
		"Expected hash from OpenText Tableau forensic imager (hardware)",
		"Expected hash from Oxygen Forensic Detective",
		"Expected hash from X-Ways Forensics",
		"Provider return hash (warrant return)",
		SumFile,
		CsvList,
		Other,
	};

	/// <summary>
	/// The source that matches a detected expected-hash format, or null when the format does not
	/// say reliably which tool wrote it.
	/// </summary>
	public static string? Suggest(string formatName, string expectedPath)
	{
		if (string.Equals(formatName, "FTK Imager text log", StringComparison.Ordinal))
		{
			string head = Head(expectedPath);
			if (head.Contains("AccessData", StringComparison.OrdinalIgnoreCase))
			{
				return FtkImagerAccessData;
			}
			if (head.Contains("Exterro", StringComparison.OrdinalIgnoreCase))
			{
				return FtkImagerExterro;
			}
			return null;
		}
		if (string.Equals(formatName, "CSV", StringComparison.Ordinal) || string.Equals(formatName, "TSV", StringComparison.Ordinal))
		{
			return CsvList;
		}
		if (formatName.Contains("sum", StringComparison.OrdinalIgnoreCase))
		{
			return SumFile;
		}
		return null;
	}

	private static string Head(string path)
	{
		try
		{
			using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			byte[] buffer = new byte[4096];
			int read = stream.Read(buffer, 0, buffer.Length);
			return Encoding.UTF8.GetString(buffer, 0, read);
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
		{
			return "";
		}
	}
}
