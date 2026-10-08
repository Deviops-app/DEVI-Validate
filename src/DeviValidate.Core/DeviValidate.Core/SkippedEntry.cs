using System.Text.Json.Serialization;

namespace DeviValidate.Core;

public sealed class SkippedEntry
{
	[JsonPropertyName("path")]
	public string Path { get; set; } = "";


	[JsonPropertyName("reason")]
	public string Reason { get; set; } = "";

}
