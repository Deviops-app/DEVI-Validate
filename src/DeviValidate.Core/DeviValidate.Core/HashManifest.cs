using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeviValidate.Core;

/// <summary>Hashes computed for an evidence file or folder.</summary>
public sealed class HashManifest : IStampedRecord
{
	[JsonPropertyName("schemaVersion")]
	public int SchemaVersion { get; set; } = 1;


	[JsonPropertyName("kind")]
	public string Kind { get; set; } = "hash-manifest";


	[JsonPropertyName("tool")]
	public string Tool { get; set; } = "DEVI Validate";


	[JsonPropertyName("version")]
	public string Version { get; set; } = ToolInfo.Version;


	[JsonPropertyName("createdAt")]
	public DateTimeOffset CreatedAt { get; set; }

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

	[JsonPropertyName("algorithm")]
	public string Algorithm { get; set; } = "";


	[JsonPropertyName("sourcePath")]
	public string SourcePath { get; set; } = "";


	[JsonPropertyName("files")]
	public List<FileHashEntry> Files { get; set; } = new List<FileHashEntry>();


	[JsonPropertyName("skipped")]
	public List<SkippedEntry> Skipped { get; set; } = new List<SkippedEntry>();


	[JsonPropertyName("integrity")]
	public ReportIntegrity Integrity { get; set; } = new ReportIntegrity();

}
