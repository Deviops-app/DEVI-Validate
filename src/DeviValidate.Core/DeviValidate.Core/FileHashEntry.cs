using System.Text.Json.Serialization;

namespace DeviValidate.Core;

public sealed class FileHashEntry
{
	[JsonPropertyName("path")]
	public string Path { get; set; } = "";


	[JsonPropertyName("size")]
	public long Size { get; set; }

	[JsonPropertyName("hash")]
	public string Hash { get; set; } = "";

}
