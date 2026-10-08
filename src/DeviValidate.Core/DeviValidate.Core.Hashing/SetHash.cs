using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DeviValidate.Core.Expected;

namespace DeviValidate.Core.Hashing;

/// <summary>
/// One hash for every file that was read in a folder.
/// The bytes are UTF-8 with no byte-order mark. Each line is the relative path,
/// a tab, the lowercase hash, and a line feed. Lines are sorted by path.
/// The same files in any order produce the same set hash.
/// Paths that were expected but not read are not included.
/// </summary>
public static class SetHash
{
	public static string Compute(IEnumerable<(string Path, string Hash)> files, HashAlgorithmKind algorithm)
	{
		string s = string.Concat(from line in files.Select<(string, string), string>(((string Path, string Hash) file) => PathKeys.Normalize(file.Path) + "\t" + HashAlgorithms.NormalizeHex(file.Hash)).OrderBy<string, string>((string line) => line, StringComparer.Ordinal)
			select line + "\n");
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithms.ToName(algorithm));
		incrementalHash.AppendData(bytes);
		return Convert.ToHexString(incrementalHash.GetHashAndReset()).ToLowerInvariant();
	}
}
