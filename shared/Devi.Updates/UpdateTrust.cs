using System.Security.Cryptography;

namespace Devi.Updates;

/// <summary>
/// Publisher material for the optional update feed shared by every DEVI Windows app.
/// Tool work that does not need the network does not read this.
/// </summary>
public static class UpdateTrust
{
	/// <summary>ECDSA P-256 public key, SubjectPublicKeyInfo, base64. The private key is not in source.</summary>
	public const string PublicKeySpkiBase64 = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEJtoj4Kz62luwAxyLoQEhc0GZHUh6wefGuU+lI5A6UfDyfQ6E57W4cy0cCDm27PpHmUdhOOcGm0AQ1REQ87bp1g==";

	public const long MaxDownloadBytes = 536870912L;

	public const long MaxFeedBytes = 1048576L;

	/// <summary>
	/// Only these hosts are accepted for feed and file URLs.
	/// </summary>
	public static readonly string[] AllowedHosts = ["downloads.deviops.app", "d3hndsq98t3yll.cloudfront.net"];

	public static Uri[] FeedUrisFor(string productId)
	{
		EnsureProductId(productId);
		return
		[
			new Uri("https://downloads.deviops.app/downloads/" + productId + "/updates.json"),
			new Uri("https://d3hndsq98t3yll.cloudfront.net/downloads/" + productId + "/updates.json")
		];
	}

	public static Uri FeedUriFor(string productId) => FeedUrisFor(productId)[0];

	public static ECDsa CreatePublicKey()
	{
		ECDsa key = ECDsa.Create();
		key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKeySpkiBase64), out _);
		return key;
	}

	public static void EnsureProductId(string productId)
	{
		if (string.IsNullOrWhiteSpace(productId)
			|| !productId.StartsWith("devi-", StringComparison.Ordinal)
			|| productId.Length > 40
			|| productId.Any(c => !(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-')))
		{
			throw new UpdateException("The product id is not a DEVI tool id.");
		}
	}
}
