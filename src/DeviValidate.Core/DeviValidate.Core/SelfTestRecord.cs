using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeviValidate.Core;

/// <summary>Result of the built-in self-test. This is not an evidence verification.</summary>
public sealed class SelfTestRecord : IStampedRecord
{
	[JsonPropertyName("schemaVersion")]
	public int SchemaVersion { get; set; } = 1;


	[JsonPropertyName("kind")]
	public string Kind { get; set; } = "tool-validation-record";


	[JsonPropertyName("tool")]
	public string Tool { get; set; } = "DEVI Validate";


	[JsonPropertyName("version")]
	public string Version { get; set; } = ToolInfo.Version;


	[JsonPropertyName("buildHash")]
	public string? BuildHash { get; set; }

	[JsonPropertyName("operatingSystem")]
	public string OperatingSystem { get; set; } = "";


	[JsonPropertyName("testedAt")]
	public DateTimeOffset TestedAt { get; set; }

	[JsonPropertyName("timeZone")]
	public string TimeZone { get; set; } = "";


	[JsonPropertyName("machineName")]
	public string MachineName { get; set; } = "";


	[JsonPropertyName("validatedBy")]
	public string? ValidatedBy { get; set; }

	[JsonPropertyName("validationDate")]
	public string? ValidationDate { get; set; }

	[JsonPropertyName("labProcedure")]
	public string? LabProcedure { get; set; }

	[JsonPropertyName("passedCount")]
	public int PassedCount { get; set; }

	[JsonPropertyName("failedCount")]
	public int FailedCount { get; set; }

	[JsonPropertyName("result")]
	public string Result { get; set; } = "";


	[JsonPropertyName("checks")]
	public List<SelfTestCheck> Checks { get; set; } = new List<SelfTestCheck>();


	[JsonPropertyName("integrity")]
	public ReportIntegrity Integrity { get; set; } = new ReportIntegrity();

}
