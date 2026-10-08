using System.Text.Json.Serialization;

namespace DeviValidate.Core;

public sealed class SelfTestCheck
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";


	[JsonPropertyName("input")]
	public string Input { get; set; } = "";


	[JsonPropertyName("expected")]
	public string Expected { get; set; } = "";


	[JsonPropertyName("computed")]
	public string Computed { get; set; } = "";


	[JsonPropertyName("passed")]
	public bool Passed { get; set; }
}
