using System.IO;
using DeviValidate.Core.Expected;
using DeviValidate.Core.Hashing;
using Xunit;

namespace DeviValidate.Core.Tests;

public sealed class ExpectedHashSetsTests
{
	[Fact]
	public void Several_hash_files_are_joined_when_they_use_one_algorithm()
	{
		ExpectedHashes first = Sample("CSV", "a.bin");
		ExpectedHashes second = Sample("sum", "b.bin");

		ExpectedHashes combined = ExpectedHashSets.Combine(new[] { first, second });

		Assert.Equal("several files", combined.FormatName);
		Assert.Equal(HashAlgorithmKind.Sha256, combined.Algorithm);
		Assert.Equal(new[] { "a.bin", "b.bin" }, new[] { combined.Entries[0].Path, combined.Entries[1].Path });
		Assert.Same(first, ExpectedHashSets.Combine(new[] { first }));
	}

	[Fact]
	public void Different_algorithms_are_refused()
	{
		ExpectedHashes sha = Sample("CSV", "a.bin");
		ExpectedHashes md5 = new ExpectedHashes
		{
			FormatName = "CSV",
			Algorithm = HashAlgorithmKind.Md5,
			Entries = new[]
			{
				new ExpectedHashEntry
				{
					Path = "b.bin",
					Hash = new string('b', 32),
					Algorithm = HashAlgorithmKind.Md5
				}
			}
		};

		InvalidDataException error = Assert.Throws<InvalidDataException>(() => ExpectedHashSets.Combine(new[] { sha, md5 }));
		Assert.Equal("The expected hashes are not all SHA-256.", error.Message);
	}

	private static ExpectedHashes Sample(string format, string path)
	{
		return new ExpectedHashes
		{
			FormatName = format,
			Algorithm = HashAlgorithmKind.Sha256,
			Entries = new[]
			{
				new ExpectedHashEntry
				{
					Path = path,
					Hash = new string('a', 64),
					Algorithm = HashAlgorithmKind.Sha256
				}
			}
		};
	}
}
