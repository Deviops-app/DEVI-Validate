using System;
using System.Collections.Generic;

namespace DeviValidate.Core.Reporting;

public static class RecordFields
{
	public readonly record struct Field(string Label, string Value, bool Mono);

	public static IReadOnlyList<Field> Verification(VerificationRecord record)
	{
		List<Field> list = new List<Field>
		{
			new Field("Evidence path", record.EvidencePath, Mono: false),
			new Field("Kind", KindLabel(record.EvidenceKind), Mono: false),
			new Field("Algorithm", record.Algorithm, Mono: false),
			new Field("Expected hashes", ExpectedSourceLabels.Describe(record.ExpectedFormat), Mono: false)
		};
		if (!string.Equals(record.ExpectedSource, "pasted hash", StringComparison.Ordinal))
		{
			list.Add(new Field("Expected file", record.ExpectedSource, Mono: false));
		}
		list.Add(new Field("Verified", TimeDisplay.Format(record.VerifiedAt, record.TimeZone), Mono: false));
		list.Add(new Field("Machine", ReportCopy.OrNotRecorded(record.MachineName), Mono: false));
		list.Add(new Field("Examiner", ReportCopy.OrNotRecorded(record.Examiner), Mono: false));
		list.Add(new Field("Agency/department", ReportCopy.OrNotRecorded(record.Agency), Mono: false));
		list.Add(new Field("Case or reference", ReportCopy.OrNotRecorded(record.CaseReference), Mono: false));
		list.Add(new Field("Validated by", ReportCopy.OrNotRecorded(record.ValidatedBy), Mono: false));
		list.Add(new Field("Validation date", ReportCopy.OrNotRecorded(record.ValidationDate), Mono: false));
		list.Add(new Field("Lab procedure/reference", ReportCopy.OrNotRecorded(record.LabProcedure), Mono: false));
		if (!string.IsNullOrWhiteSpace(record.SetHash))
		{
			list.Add(new Field("Set hash", record.SetHash, Mono: true));
		}
		return list;
	}

	private static string KindLabel(string kind)
	{
		if (string.Equals(kind, "folder", StringComparison.Ordinal))
		{
			return "Folder";
		}
		if (string.Equals(kind, "files", StringComparison.Ordinal))
		{
			return "Files";
		}
		return "File";
	}

	public static IReadOnlyList<Field> SelfTest(SelfTestRecord record)
	{
		return System.Array.AsReadOnly(new Field[8]
		{
			new Field("Tool version", record.Version, Mono: false),
			new Field("Build hash", ReportCopy.OrNotRecorded(record.BuildHash), Mono: true),
			new Field("Operating system", ReportCopy.OrNotRecorded(record.OperatingSystem), Mono: false),
			new Field("Tested", TimeDisplay.Format(record.TestedAt, record.TimeZone), Mono: false),
			new Field("Machine", ReportCopy.OrNotRecorded(record.MachineName), Mono: false),
			new Field("Validated by", ReportCopy.OrNotRecorded(record.ValidatedBy), Mono: false),
			new Field("Validation date", ReportCopy.OrNotRecorded(record.ValidationDate), Mono: false),
			new Field("Lab procedure/reference", ReportCopy.OrNotRecorded(record.LabProcedure), Mono: false)
		});
	}

	public static string FileNameNote(VerificationRecord record)
	{
		if (!string.IsNullOrWhiteSpace(record.SetHash))
		{
			return "The set hash is the named algorithm applied to the sorted list of relative path, tab, and lowercase hash for each file that was read. Exported files use this set hash as the file name.";
		}
		return "Exported files use the computed hash of this file as the file name.";
	}
}
