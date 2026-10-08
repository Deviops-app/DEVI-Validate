using System;
using System.Globalization;
using DeviValidate.Core;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Cli;

internal static class ConsoleReport
{
	public static void WriteManifest(HashManifest manifest, string? writtenPath)
	{
		Console.WriteLine("DEVI Validate 1.0.3");
		Console.WriteLine();
		Field("Source", manifest.SourcePath);
		Field("Algorithm", manifest.Algorithm);
		Field("Computed", TimeDisplay.Format(manifest.CreatedAt, manifest.TimeZone));
		Field("Machine", manifest.MachineName);
		Field("Examiner", manifest.Examiner);
		Field("Case", manifest.CaseReference);
		Field("Files", manifest.Files.Count.ToString(CultureInfo.InvariantCulture));
		if (manifest.Skipped.Count > 0)
		{
			Field("Skipped", ReportCopy.SkippedSummary(manifest.Skipped.Count));
			foreach (SkippedEntry item in manifest.Skipped)
			{
				Console.WriteLine("  " + item.Path);
				Console.WriteLine("    " + ReportCopy.SkippedDetail(item));
			}
		}
		Console.WriteLine();
		foreach (FileHashEntry file in manifest.Files)
		{
			Console.WriteLine(file.Path);
			Console.WriteLine("  " + ByteSize.Format(file.Size));
			Console.WriteLine("  " + file.Hash);
		}
		if (manifest.Files.Count == 0)
		{
			Console.WriteLine("No files were hashed.");
		}
		Console.WriteLine();
		if (writtenPath == null)
		{
			Console.WriteLine("No manifest written. Pass --output to write JSON outside the evidence path.");
		}
		else
		{
			Field("Manifest", writtenPath);
		}
		Field("Record SHA-256", manifest.Integrity.Hash);
	}

	public static void WriteVerification(VerificationRecord record, WrittenReports? written)
	{
		Console.WriteLine("DEVI Validate 1.0.3");
		Console.WriteLine();
		Field("Evidence", record.EvidencePath);
		Field("Algorithm", record.Algorithm);
		Field("Expected", ExpectedSourceLabels.Describe(record.ExpectedFormat));
		if (!string.Equals(record.ExpectedSource, "pasted hash", StringComparison.Ordinal))
		{
			Field("Expected file", record.ExpectedSource);
		}
		Field("Verified", TimeDisplay.Format(record.VerifiedAt, record.TimeZone));
		Field("Machine", record.MachineName);
		Field("Examiner", record.Examiner);
		Field("Case", record.CaseReference);
		Field("Validated by", ReportCopy.OrNotRecorded(record.ValidatedBy));
		Field("Validation date", ReportCopy.OrNotRecorded(record.ValidationDate));
		Field("Lab procedure", ReportCopy.OrNotRecorded(record.LabProcedure));
		if (!string.IsNullOrWhiteSpace(record.SetHash))
		{
			Field("Set hash", record.SetHash);
			Console.WriteLine("The set hash is the named algorithm applied to the sorted list of relative path, tab, and lowercase hash for each file that was read. Exported files use this set hash as the file name.");
		}
		Console.WriteLine();
		foreach (FileVerificationEntry file in record.Files)
		{
			Console.WriteLine($"{ResultLabels.Display(file.Result).ToUpperInvariant(),-10} {file.Path}");
			long? size = file.Size;
			if (size.HasValue)
			{
				long valueOrDefault = size.GetValueOrDefault();
				Console.WriteLine("           " + ByteSize.Format(valueOrDefault));
			}
			if (file.Result == "mismatch")
			{
				Console.WriteLine("           computed  " + file.ComputedHash);
				Console.WriteLine("           expected  " + file.ExpectedHash);
			}
			else if (!string.IsNullOrEmpty(file.ComputedHash))
			{
				Console.WriteLine("           " + file.ComputedHash);
			}
			else if (!string.IsNullOrEmpty(file.ExpectedHash))
			{
				Console.WriteLine("           expected  " + file.ExpectedHash);
			}
		}
		if (record.Skipped.Count > 0)
		{
			Console.WriteLine();
			Console.WriteLine(ReportCopy.SkippedSummary(record.Skipped.Count));
			foreach (SkippedEntry item in record.Skipped)
			{
				Console.WriteLine("  " + item.Path);
				Console.WriteLine("    " + ReportCopy.SkippedDetail(item));
			}
		}
		Console.WriteLine();
		Console.WriteLine($"Match {record.MatchCount.ToString(CultureInfo.InvariantCulture)}    Mismatch {record.MismatchCount.ToString(CultureInfo.InvariantCulture)}    Missing {record.MissingCount.ToString(CultureInfo.InvariantCulture)}    Extra {record.ExtraCount.ToString(CultureInfo.InvariantCulture)}");
		Console.WriteLine();
		Console.WriteLine("Verdict");
		if (record.MismatchCount > 0 && !Console.IsOutputRedirected)
		{
			Console.Write("\u001b[31m");
			Console.WriteLine(record.Verdict);
			Console.WriteLine("Red means the new hash is not the same as the hash written down before. The file we read is not the same as the file that hash describes. The notes below say what that can and cannot mean.");
			Console.Write("\u001b[0m");
		}
		else
		{
			Console.WriteLine(record.Verdict);
			if (record.MismatchCount > 0)
			{
				Console.WriteLine("Red means the new hash is not the same as the hash written down before. The file we read is not the same as the file that hash describes. The notes below say what that can and cannot mean.");
			}
		}
		if (record.MismatchCount == 0 && record.MissingCount == 0 && record.ExtraCount == 0)
		{
			Console.WriteLine("A match means the new hash is the same as the hash written down before. The file we read is the same file that hash describes. Anyone can check the file again with sha256sum, certutil -hashfile, or Get-FileHash. This check verifies the bytes only. It does not show who made the file, what the file means, or whether a chain of custody is complete.");
		}
		if (!string.IsNullOrWhiteSpace(record.Note))
		{
			Console.WriteLine();
			Console.WriteLine(record.Note);
		}
		Console.WriteLine();
		if ((object)written == null || (written.HtmlPath == null && written.JsonPath == null && written.PdfPath == null))
		{
			Console.WriteLine("No report written. Pass --output-dir to write HTML, JSON, and PDF outside the evidence path.");
		}
		else
		{
			Field("HTML", written.HtmlPath);
			Field("JSON", written.JsonPath);
			Field("PDF", written.PdfPath);
		}
		Field("Record SHA-256", record.Integrity.Hash);
	}

	public static void WriteSelfTest(SelfTestRecord record, WrittenReports written)
	{
		Console.WriteLine("DEVI Validate 1.0.3");
		Console.WriteLine("Tool validation record");
		Console.WriteLine();
		Console.WriteLine(record.Result);
		Console.WriteLine();
		Field("Version", record.Version);
		Field("Build hash", ReportCopy.OrNotRecorded(record.BuildHash));
		Field("OS", record.OperatingSystem);
		Field("Tested", TimeDisplay.Format(record.TestedAt, record.TimeZone));
		Field("Machine", record.MachineName);
		Field("Validated by", ReportCopy.OrNotRecorded(record.ValidatedBy));
		Field("Validation date", ReportCopy.OrNotRecorded(record.ValidationDate));
		Field("Lab procedure", ReportCopy.OrNotRecorded(record.LabProcedure));
		Console.WriteLine();
		foreach (SelfTestCheck check in record.Checks)
		{
			Console.WriteLine($"{(check.Passed ? "PASS" : "FAIL"),-6} {check.Name}  {check.Input}");
			Console.WriteLine("       expected  " + check.Expected);
			Console.WriteLine("       computed  " + check.Computed);
		}
		Console.WriteLine();
		Field("PDF", written.PdfPath);
		Field("HTML", written.HtmlPath);
		Field("JSON", written.JsonPath);
		Field("Record SHA-256", record.Integrity.Hash);
	}

	private static void Field(string label, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			Console.WriteLine($"{label,-12} {value}");
		}
	}
}
