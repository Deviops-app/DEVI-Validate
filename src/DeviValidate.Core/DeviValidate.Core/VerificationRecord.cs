using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeviValidate.Core;

/// <summary>Comparison of computed hashes with an expected list.</summary>
public sealed class VerificationRecord : IStampedRecord
{
	[JsonPropertyName("schemaVersion")]
	public int SchemaVersion { get; set; } = 1;


	[JsonPropertyName("kind")]
	public string Kind { get; set; } = "verification-record";


	[JsonPropertyName("tool")]
	public string Tool { get; set; } = "DEVI Validate";


	[JsonPropertyName("version")]
	public string Version { get; set; } = ToolInfo.Version;


	[JsonPropertyName("verifiedAt")]
	public DateTimeOffset VerifiedAt { get; set; }

	[JsonPropertyName("timeZone")]
	public string TimeZone { get; set; } = "";


	[JsonPropertyName("machineName")]
	public string MachineName { get; set; } = "";


	[JsonPropertyName("examiner")]
	public string? Examiner { get; set; }

	/// <summary>Agency or department. Written only when recorded, so records without it keep their canonical form.</summary>
	[JsonPropertyName("agency")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Agency { get; set; }

	[JsonPropertyName("caseReference")]
	public string? CaseReference { get; set; }

	[JsonPropertyName("validatedBy")]
	public string? ValidatedBy { get; set; }

	[JsonPropertyName("validationDate")]
	public string? ValidationDate { get; set; }

	[JsonPropertyName("labProcedure")]
	public string? LabProcedure { get; set; }

	[JsonPropertyName("algorithm")]
	public string Algorithm { get; set; } = "";


	[JsonPropertyName("evidencePath")]
	public string EvidencePath { get; set; } = "";


	/// <summary><c>file</c> or <c>folder</c>.</summary>
	[JsonPropertyName("evidenceKind")]
	public string EvidenceKind { get; set; } = "";


	/// <summary>Present when the evidence is a folder. Null for a single file.</summary>
	[JsonPropertyName("setHash")]
	public string? SetHash { get; set; }

	[JsonPropertyName("expectedSource")]
	public string ExpectedSource { get; set; } = "";


	[JsonPropertyName("expectedFormat")]
	public string ExpectedFormat { get; set; } = "";


	[JsonPropertyName("note")]
	public string? Note { get; set; }

	[JsonPropertyName("matchCount")]
	public int MatchCount { get; set; }

	[JsonPropertyName("mismatchCount")]
	public int MismatchCount { get; set; }

	[JsonPropertyName("missingCount")]
	public int MissingCount { get; set; }

	[JsonPropertyName("extraCount")]
	public int ExtraCount { get; set; }

	[JsonPropertyName("fileCount")]
	public int FileCount { get; set; }

	[JsonPropertyName("verdict")]
	public string Verdict { get; set; } = "";


	[JsonPropertyName("files")]
	public List<FileVerificationEntry> Files { get; set; } = new List<FileVerificationEntry>();


	[JsonPropertyName("skipped")]
	public List<SkippedEntry> Skipped { get; set; } = new List<SkippedEntry>();


	[JsonPropertyName("integrity")]
	public ReportIntegrity Integrity { get; set; } = new ReportIntegrity();

}
