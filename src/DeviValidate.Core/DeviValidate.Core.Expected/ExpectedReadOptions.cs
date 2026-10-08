using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected;

public sealed class ExpectedReadOptions
{
	public HashAlgorithmKind? Algorithm { get; init; }

	public string? PathColumn { get; init; }

	public string? HashColumn { get; init; }

	/// <summary>auto, sum, csv, tsv, manifest, or ftk.</summary>
	public string? Format { get; init; }
}
