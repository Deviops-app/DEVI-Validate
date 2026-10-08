using System;
using System.Linq;
using System.Text;

namespace DeviValidate.Core.Packaging;

/// <summary>
/// The short verification string printed on a validation package and encoded in its QR code.
/// It is taken from the verification record's own integrity hash (SHA-256 of the canonical record
/// JSON), so anyone holding the JSON can recompute it, with or without this tool.
/// </summary>
public static class VerificationCode
{
	public const string Prefix = "DV1";

	/// <summary>QR payload. Carries the full 64-character record hash so a phone scan gives the whole value.</summary>
	public static string QrPayload(string recordHash) => "DEVI-VALIDATE:1:" + Normalize(recordHash);

	/// <summary>DV1-XXXX-XXXX-XXXX-XXXX-XXXX: the first 80 bits of the record hash, upper-case hex in groups of four.</summary>
	public static string FromRecordHash(string recordHash)
	{
		string hex = Normalize(recordHash);
		if (hex.Length != 64 || !hex.All(Uri.IsHexDigit))
		{
			throw new ArgumentException("A record hash is 64 hexadecimal characters.", nameof(recordHash));
		}
		var sb = new StringBuilder(Prefix);
		for (int i = 0; i < 20; i += 4)
		{
			sb.Append('-').Append(hex.Substring(i, 4).ToUpperInvariant());
		}
		return sb.ToString();
	}

	/// <summary>
	/// True when <paramref name="entered"/> identifies <paramref name="recordHash"/>. Accepts the short
	/// code (any case, with or without dashes or the DV1 prefix), the full 64-character hash, or the QR payload.
	/// </summary>
	public static bool Matches(string entered, string recordHash)
	{
		string hash = Normalize(recordHash);
		string text = entered.Trim();
		if (text.StartsWith("DEVI-VALIDATE:1:", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring("DEVI-VALIDATE:1:".Length);
		}
		if (text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(Prefix.Length);
		}
		string compact = new string(text.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToLowerInvariant();
		if (compact.Length == 64)
		{
			return compact == hash;
		}
		return compact.Length == 20 && hash.StartsWith(compact, StringComparison.Ordinal);
	}

	private static string Normalize(string hash) => (hash ?? "").Trim().ToLowerInvariant();
}
