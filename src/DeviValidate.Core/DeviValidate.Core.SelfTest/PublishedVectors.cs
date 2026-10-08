using System.Collections.Generic;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.SelfTest;

/// <summary>
/// Published test vectors. SHA-256 and SHA-1 values are the FIPS 180-4 examples.
/// MD5 values are the RFC 1321 examples.
/// </summary>
public static class PublishedVectors
{
	public readonly record struct Vector(string Algorithm, string Input, HashAlgorithmKind Kind, string Text, string Expected);

	public static IReadOnlyList<Vector> All { get; } = System.Array.AsReadOnly(new Vector[7]
	{
		new Vector("SHA-256", "empty string", HashAlgorithmKind.Sha256, "", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"),
		new Vector("SHA-256", "ASCII \"abc\"", HashAlgorithmKind.Sha256, "abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"),
		new Vector("SHA-256", "FIPS 180-4 one-block message", HashAlgorithmKind.Sha256, "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1"),
		new Vector("SHA-1", "empty string", HashAlgorithmKind.Sha1, "", "da39a3ee5e6b4b0d3255bfef95601890afd80709"),
		new Vector("SHA-1", "ASCII \"abc\"", HashAlgorithmKind.Sha1, "abc", "a9993e364706816aba3e25717850c26c9cd0d89d"),
		new Vector("MD5", "empty string", HashAlgorithmKind.Md5, "", "d41d8cd98f00b204e9800998ecf8427e"),
		new Vector("MD5", "ASCII \"abc\"", HashAlgorithmKind.Md5, "abc", "900150983cd24fb0d6963f7d28e17f72")
	});

}
