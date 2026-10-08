using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected;

public sealed class ExpectedHashEntry
{
	public required string Path { get; init; }

	public required string Hash { get; init; }

	public required HashAlgorithmKind Algorithm { get; init; }

	public long? Size { get; init; }
}
