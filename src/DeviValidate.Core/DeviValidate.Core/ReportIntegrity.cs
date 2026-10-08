using System.Text.Json.Serialization;

namespace DeviValidate.Core;

public sealed class ReportIntegrity
{
	[JsonPropertyName("algorithm")]
	public string Algorithm { get; set; } = "SHA-256";


	[JsonPropertyName("scope")]
	public string Scope { get; set; } = "";


	[JsonPropertyName("hash")]
	public string Hash { get; set; } = "";

}
