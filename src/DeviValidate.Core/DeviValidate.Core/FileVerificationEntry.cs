using System.Text.Json.Serialization;

namespace DeviValidate.Core;

public sealed class FileVerificationEntry
{
	[JsonPropertyName("path")]
	public string Path { get; set; } = "";


	[JsonPropertyName("size")]
	public long? Size { get; set; }

	[JsonPropertyName("expectedSize")]
	public long? ExpectedSize { get; set; }

	[JsonPropertyName("computedHash")]
	public string? ComputedHash { get; set; }

	[JsonPropertyName("expectedHash")]
	public string? ExpectedHash { get; set; }

	[JsonPropertyName("result")]
	public string Result { get; set; } = "";

}
