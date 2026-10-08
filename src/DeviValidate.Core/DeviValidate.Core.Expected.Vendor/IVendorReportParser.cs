using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected.Vendor;

/// <summary>
/// Extension point for an acquisition-report parser.
/// Built-in support does not include Cellebrite, Magnet AXIOM, or hashes stored inside E01 files.
/// Add a class that implements this interface and register it from <see cref="M:DeviValidate.Core.Expected.Vendor.VendorParserRegistry.RegisterBuiltIns" />.
/// The application does not load parser assemblies from disk.
/// </summary>
public interface IVendorReportParser
{
	string Name { get; }

	bool CanParse(string content, string? fileName);

	ExpectedHashes Parse(string content, HashAlgorithmKind? algorithm);
}
