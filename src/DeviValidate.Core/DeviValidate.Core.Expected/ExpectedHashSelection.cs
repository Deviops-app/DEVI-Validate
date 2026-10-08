using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected;

public static class ExpectedHashSelection
{
	public static ExpectedHashes Strict(string format, string? note, IReadOnlyList<ExpectedHashEntry> entries, HashAlgorithmKind? requested)
	{
		if (entries.Count == 0)
		{
			throw new InvalidDataException("The expected file did not contain any hashes.");
		}
		if (requested.HasValue)
		{
			HashAlgorithmKind kind = requested.GetValueOrDefault();
			ExpectedHashEntry expectedHashEntry = entries.FirstOrDefault((ExpectedHashEntry entry) => entry.Algorithm != kind);
			if (expectedHashEntry != null)
			{
				string value = (string.IsNullOrEmpty(expectedHashEntry.Path) ? "a hash" : ("'" + expectedHashEntry.Path + "'"));
				throw new InvalidDataException($"Expected a {HashAlgorithms.DisplayName(kind)} hash, and {value} is {HashAlgorithms.DisplayName(expectedHashEntry.Algorithm)}.");
			}
			return new ExpectedHashes
			{
				FormatName = format,
				Note = note,
				Algorithm = kind,
				Entries = entries
			};
		}
		List<HashAlgorithmKind> list = entries.Select((ExpectedHashEntry entry) => entry.Algorithm).Distinct().ToList();
		if (list.Count != 1)
		{
			throw new InvalidDataException("The expected file contains more than one hash algorithm. Pass --algorithm to choose one.");
		}
		return new ExpectedHashes
		{
			FormatName = format,
			Note = note,
			Algorithm = list[0],
			Entries = entries
		};
	}

	public static ExpectedHashes Prefer(string format, string? note, IReadOnlyList<ExpectedHashEntry> entries, HashAlgorithmKind? requested)
	{
		if (entries.Count == 0)
		{
			throw new InvalidDataException("The expected file did not contain any hashes.");
		}
		HashAlgorithmKind chosen = requested ?? PreferKind(entries);
		List<ExpectedHashEntry> list = entries.Where((ExpectedHashEntry entry) => entry.Algorithm == chosen).ToList();
		if (list.Count == 0)
		{
			throw new InvalidDataException("The expected file has no " + HashAlgorithms.DisplayName(chosen) + " hashes.");
		}
		return new ExpectedHashes
		{
			FormatName = format,
			Note = note,
			Algorithm = chosen,
			Entries = list
		};
	}

	private static HashAlgorithmKind PreferKind(IReadOnlyList<ExpectedHashEntry> entries)
	{
		HashAlgorithmKind[] array = new HashAlgorithmKind[3]
		{
			HashAlgorithmKind.Sha256,
			HashAlgorithmKind.Sha1,
			HashAlgorithmKind.Md5
		};
		foreach (HashAlgorithmKind kind in array)
		{
			if (entries.Any((ExpectedHashEntry entry) => entry.Algorithm == kind))
			{
				return kind;
			}
		}
		throw new InvalidDataException("The expected file did not contain a SHA-256, SHA-1, or MD5 hash.");
	}
}
