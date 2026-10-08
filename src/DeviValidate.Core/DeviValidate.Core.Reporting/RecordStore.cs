using System;
using System.IO;
using System.Text.Json;
using DeviValidate.Core.IO;

namespace DeviValidate.Core.Reporting;

public static class RecordStore
{
	public static string WriteManifest(string evidencePath, string outputFile, HashManifest manifest)
	{
		string text = ExportNames.Unused(outputFile);
		TextFiles.WriteOutside(evidencePath, text, CanonicalJson.ToPretty(manifest) + Environment.NewLine);
		return Path.GetFullPath(text);
	}

	public static WrittenReports WriteVerification(string evidencePath, string outputDirectory, VerificationRecord record)
	{
		Directory.CreateDirectory(outputDirectory);
		string[] array = ExportNames.UniqueSet(outputDirectory, ExportNames.Stem(record), ".html", ".json", ".pdf");
		TextFiles.WriteOutside(evidencePath, array[0], HtmlReport.Render(record));
		TextFiles.WriteOutside(evidencePath, array[1], CanonicalJson.ToPretty(record) + Environment.NewLine);
		TextFiles.WriteBytesOutside(evidencePath, array[2], PdfReport.Render(record));
		return new WrittenReports(Path.GetFullPath(array[0]), Path.GetFullPath(array[1]), Path.GetFullPath(array[2]));
	}

	public static string WriteVerificationJson(string evidencePath, string outputFile, VerificationRecord record)
	{
		string text = ExportNames.Unused(outputFile);
		TextFiles.WriteOutside(evidencePath, text, CanonicalJson.ToPretty(record) + Environment.NewLine);
		return Path.GetFullPath(text);
	}

	public static string WriteVerificationHtml(string evidencePath, string outputFile, VerificationRecord record)
	{
		string text = ExportNames.Unused(outputFile);
		TextFiles.WriteOutside(evidencePath, text, HtmlReport.Render(record));
		return Path.GetFullPath(text);
	}

	public static string WriteVerificationPdf(string evidencePath, string outputFile, VerificationRecord record)
	{
		string text = ExportNames.Unused(outputFile);
		TextFiles.WriteBytesOutside(evidencePath, text, PdfReport.Render(record));
		return Path.GetFullPath(text);
	}

	public static string WriteManifestHtml(string evidencePath, string outputFile, HashManifest manifest)
	{
		string text = ExportNames.Unused(outputFile);
		TextFiles.WriteOutside(evidencePath, text, HtmlReport.Render(manifest));
		return Path.GetFullPath(text);
	}

	public static string WriteSelfTestHtml(string? evidencePath, string outputFile, SelfTestRecord record)
	{
		string text = ExportNames.Unused(outputFile);
		TextFiles.WriteOutside(evidencePath, text, HtmlReport.Render(record));
		return Path.GetFullPath(text);
	}

	public static string WriteSelfTestJson(string? evidencePath, string outputFile, SelfTestRecord record)
	{
		string text = ExportNames.Unused(outputFile);
		TextFiles.WriteOutside(evidencePath, text, CanonicalJson.ToPretty(record) + Environment.NewLine);
		return Path.GetFullPath(text);
	}

	public static string WriteSelfTestPdf(string? evidencePath, string outputFile, SelfTestRecord record)
	{
		string text = ExportNames.Unused(outputFile);
		TextFiles.WriteBytesOutside(evidencePath, text, PdfReport.Render(record));
		return Path.GetFullPath(text);
	}

	public static WrittenReports WriteSelfTest(string? evidencePath, string outputDirectory, SelfTestRecord record)
	{
		Directory.CreateDirectory(outputDirectory);
		string stem = ExportNames.SelfTestStem(record.Version, record.TestedAt);
		string[] array = ExportNames.UniqueSet(outputDirectory, stem, ".pdf", ".html", ".json");
		TextFiles.WriteBytesOutside(evidencePath, array[0], PdfReport.Render(record));
		TextFiles.WriteOutside(evidencePath, array[1], HtmlReport.Render(record));
		TextFiles.WriteOutside(evidencePath, array[2], CanonicalJson.ToPretty(record) + Environment.NewLine);
		return new WrittenReports(Path.GetFullPath(array[1]), Path.GetFullPath(array[2]), Path.GetFullPath(array[0]));
	}

	public static IStampedRecord ReadStamped(string path, out string evidencePath)
	{
		string json = File.ReadAllText(path);
		string text;
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			text = (jsonDocument.RootElement.TryGetProperty("kind", out var value) ? value.GetString() : null);
		}
		catch (JsonException ex)
		{
			throw new InvalidDataException("Could not read the JSON record: " + ex.Message, ex);
		}
		switch (text)
		{
		case "hash-manifest":
		{
			HashManifest hashManifest = CanonicalJson.FromJson<HashManifest>(json);
			evidencePath = hashManifest.SourcePath;
			return hashManifest;
		}
		case "verification-record":
		{
			VerificationRecord verificationRecord = CanonicalJson.FromJson<VerificationRecord>(json);
			evidencePath = verificationRecord.EvidencePath;
			return verificationRecord;
		}
		case "tool-validation-record":
		{
			SelfTestRecord result = CanonicalJson.FromJson<SelfTestRecord>(json);
			evidencePath = "";
			return result;
		}
		default:
			throw new InvalidDataException("JSON kind must be hash-manifest, verification-record, or tool-validation-record.");
		}
	}
}
