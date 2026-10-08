using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeviValidate.Core.Hashing;

namespace DeviValidate.Core.Expected;

/// <summary>Joins expected-hash files that were chosen together.</summary>
public static class ExpectedHashSets
{
	public static ExpectedHashes Combine(IReadOnlyList<ExpectedHashes> parts)
	{
		if (parts == null || parts.Count == 0)
		{
			throw new InvalidDataException("The expected hash source did not contain any hashes.");
		}
		if (parts.Count == 1)
		{
			return parts[0];
		}
		HashAlgorithmKind algorithm = parts[0].Algorithm;
		foreach (ExpectedHashes part in parts)
		{
			if (part.Algorithm != algorithm)
			{
				throw new InvalidDataException("The expected hashes are not all " + HashAlgorithms.DisplayName(algorithm) + ".");
			}
		}
		List<string> formats = parts.Select((ExpectedHashes part) => part.FormatName).Distinct(StringComparer.Ordinal).ToList();
		return new ExpectedHashes
		{
			FormatName = (formats.Count == 1) ? formats[0] : "several files",
			Algorithm = algorithm,
			Note = parts.Select((ExpectedHashes part) => part.Note).FirstOrDefault((string? note) => !string.IsNullOrWhiteSpace(note)),
			Entries = parts.SelectMany((ExpectedHashes part) => part.Entries).ToList()
		};
	}
}
