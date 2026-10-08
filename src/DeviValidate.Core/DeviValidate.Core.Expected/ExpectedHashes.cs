using System.Collections.Generic;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected;

public sealed class ExpectedHashes
{
	public required string FormatName { get; init; }

	public string? Note { get; init; }

	public required HashAlgorithmKind Algorithm { get; init; }

	public required IReadOnlyList<ExpectedHashEntry> Entries { get; init; }
}
