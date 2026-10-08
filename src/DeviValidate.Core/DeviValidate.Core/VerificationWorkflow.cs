using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeviValidate.Core.Expected;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.Reporting;
using DeviValidate.Core.Verification;

namespace DeviValidate.Core;

/// <summary>Shared steps used by the command line and the desktop app.</summary>
public static class VerificationWorkflow
{
	public static Task<HashManifest> HashAsync(string evidencePath, HashAlgorithmKind algorithm, string? examiner, string? caseReference, IProgress<HashProgress>? progress, CancellationToken cancellationToken, string? agency = null)
	{
		return EvidenceHasher.HashAsync(evidencePath, algorithm, ExaminationContext.Capture(examiner, caseReference, agency: agency), progress, cancellationToken);
	}

	public static ExpectedHashes ReadExpected(string path, ExpectedReadOptions? options = null)
	{
		return ExpectedHashReader.ReadFile(path, options);
	}

	/// <summary>Reads one hash file, or each file in a folder of hash files. Subfolders are not walked.</summary>
	public static ExpectedHashes ReadExpectedPath(string path, ExpectedReadOptions? options = null)
	{
		if (Directory.Exists(path))
		{
			string[] files = Directory.GetFiles(path);
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);
			if (files.Length == 0)
			{
				throw new InvalidDataException("The folder does not contain a hash file.");
			}
			List<ExpectedHashes> parts = new List<ExpectedHashes>(files.Length);
			foreach (string file in files)
			{
				parts.Add(ExpectedHashReader.ReadFile(file, options));
			}
			return ExpectedHashSets.Combine(parts);
		}
		return ExpectedHashReader.ReadFile(path, options);
	}

	/// <summary>
	/// One manifest for several evidence files or folders. A single path is hashed by <see cref="HashAsync"/>.
	/// Two files that would share one relative path are refused rather than renamed.
	/// </summary>
	public static HashManifest CombineManifests(IReadOnlyList<HashManifest> parts)
	{
		if (parts == null || parts.Count == 0)
		{
			throw new InvalidOperationException("Choose an evidence file or folder.");
		}
		if (parts.Count == 1)
		{
			return parts[0];
		}
		List<FileHashEntry> files = new List<FileHashEntry>();
		List<SkippedEntry> skipped = new List<SkippedEntry>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (HashManifest part in parts)
		{
			foreach (FileHashEntry file in part.Files)
			{
				string key = PathKeys.Normalize(file.Path);
				if (!seen.Add(key))
				{
					throw new InvalidDataException("Two evidence files normalize to '" + key + "'.");
				}
				files.Add(file);
			}
			skipped.AddRange(part.Skipped);
		}
		skipped.Sort((SkippedEntry left, SkippedEntry right) => string.CompareOrdinal(left.Path, right.Path));
		HashManifest first = parts[0];
		HashManifest combined = new HashManifest
		{
			CreatedAt = first.CreatedAt,
			TimeZone = first.TimeZone,
			MachineName = first.MachineName,
			Examiner = first.Examiner,
			Agency = first.Agency,
			CaseReference = first.CaseReference,
			Algorithm = first.Algorithm,
			SourcePath = string.Join("; ", parts.Select((HashManifest part) => part.SourcePath)),
			Files = files,
			Skipped = skipped
		};
		RecordIntegrity.Stamp(combined);
		return combined;
	}

	public static ExpectedHashes SingleHash(string evidenceFilePath, string hash, HashAlgorithmKind? algorithm)
	{
		if (Directory.Exists(evidenceFilePath))
		{
			throw new InvalidOperationException("A pasted hash applies to one file. Choose a file, or pass an expected hash list for a folder.");
		}
		string text = HashAlgorithms.NormalizeHex(hash);
		HashAlgorithmKind kind;
		if (algorithm.HasValue)
		{
			HashAlgorithmKind valueOrDefault = algorithm.GetValueOrDefault();
			if (text.Length != HashAlgorithms.HexLength(valueOrDefault))
			{
				throw new InvalidDataException($"The hash is {text.Length} hex characters, and {HashAlgorithms.DisplayName(valueOrDefault)} uses {HashAlgorithms.HexLength(valueOrDefault)}.");
			}
			kind = valueOrDefault;
		}
		else if (!HashAlgorithms.TryFromHexLength(text.Length, out kind))
		{
			throw new InvalidDataException("The hash length is not MD5, SHA-1, or SHA-256.");
		}
		return new ExpectedHashes
		{
			FormatName = "single hash",
			Algorithm = kind,
			Entries = new ExpectedHashEntry[1]
			{
				new ExpectedHashEntry
				{
					Path = PathKeys.Normalize(Path.GetFileName(evidenceFilePath)),
					Hash = text,
					Algorithm = kind
				}
			}
		};
	}

	public static VerificationRecord Verify(HashManifest computed, ExpectedHashes expected, string evidencePath, string expectedSource, string? examiner, string? caseReference, bool ignorePathCase, string? validatedBy = null, string? validationDate = null, string? labProcedure = null, string? agency = null)
	{
		ExpectedHashRules.EnsureCompatible(evidencePath, expected);
		return Verifier.Verify(computed, expected, ExaminationContext.Capture(examiner, caseReference, validatedBy, validationDate, labProcedure, agency), evidencePath, expectedSource, ignorePathCase);
	}

	/// <summary>
	/// Verifies one evidence path the same way as <see cref="Verify"/>.
	/// Several paths are checked one by one, then recorded together.
	/// </summary>
	public static VerificationRecord VerifySelection(HashManifest computed, ExpectedHashes expected, IReadOnlyList<string> evidencePaths, string expectedSource, string? examiner, string? caseReference, bool ignorePathCase, string? validatedBy = null, string? validationDate = null, string? labProcedure = null, string? agency = null)
	{
		if (evidencePaths == null || evidencePaths.Count == 0)
		{
			throw new InvalidOperationException("Choose an evidence file or folder.");
		}
		if (evidencePaths.Count == 1)
		{
			return Verify(computed, expected, evidencePaths[0], expectedSource, examiner, caseReference, ignorePathCase, validatedBy, validationDate, labProcedure, agency);
		}
		foreach (string path in evidencePaths)
		{
			ExpectedHashRules.EnsureCompatible(path, expected);
		}
		string joined = string.Join("; ", evidencePaths.Select((string path) => Path.GetFullPath(path)));
		return Verifier.Verify(computed, expected, ExaminationContext.Capture(examiner, caseReference, validatedBy, validationDate, labProcedure, agency), joined, expectedSource, ignorePathCase, "files");
	}
}
