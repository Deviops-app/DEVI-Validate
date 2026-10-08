using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DeviValidate.Core.Hashing;

/// <summary>
/// Hashes a stream in fixed-size reads so a large image does not have to fit in memory.
/// </summary>
public static class StreamingHasher
{
	public static async Task<string> HashAsync(Stream stream, HashAlgorithmKind algorithm, IProgress<long>? progress, CancellationToken cancellationToken, int bufferSize = 1048576)
	{
		if (bufferSize < 1)
		{
			throw new ArgumentOutOfRangeException("bufferSize");
		}
		using IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithms.ToName(algorithm));
		byte[] buffer = new byte[bufferSize];
		long total = 0L;
		while (true)
		{
			int num = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (num == 0)
			{
				break;
			}
			hasher.AppendData(buffer, 0, num);
			total += num;
			progress?.Report(total);
		}
		if (total == 0L)
		{
			progress?.Report(total);
		}
		return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
	}
}
