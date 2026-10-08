using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeviValidate.Core.Expected;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Verification;

public static class Verifier
{
	public static VerificationRecord Verify(HashManifest computed, ExpectedHashes expected, ExaminationContext context, string evidencePath, string expectedSource, bool ignorePathCase, string? evidenceKind = null)
	{
		if (expected.Entries.Count == 0)
		{
			throw new InvalidDataException("The expected hash source did not contain any hashes.");
		}
		HashAlgorithmKind hashAlgorithmKind = HashAlgorithms.Parse(computed.Algorithm);
		if (hashAlgorithmKind != expected.Algorithm)
		{
			throw new InvalidDataException($"Computed hashes are {computed.Algorithm}, and the expected hashes are {HashAlgorithms.DisplayName(expected.Algorithm)}.");
		}
		List<FileVerificationEntry> list = Compare(computed.Files, expected.Entries, ignorePathCase);
		int matchCount = list.Count((FileVerificationEntry entry) => entry.Result == "match");
		int num = list.Count((FileVerificationEntry entry) => entry.Result == "mismatch");
		int num2 = list.Count((FileVerificationEntry entry) => entry.Result == "missing");
		int num3 = list.Count((FileVerificationEntry entry) => entry.Result == "extra");
		string text = evidenceKind ?? (Directory.Exists(evidencePath) ? "folder" : "file");
		string setHash = null;
		if (text == "folder" || text == "files")
		{
			setHash = SetHash.Compute(computed.Files.Select((FileHashEntry file) => (Path: file.Path, Hash: file.Hash)), hashAlgorithmKind);
		}
		VerificationRecord obj = new VerificationRecord
		{
			VerifiedAt = context.Timestamp,
			TimeZone = context.TimeZoneId,
			MachineName = context.MachineName,
			Examiner = context.Examiner,
			Agency = context.Agency,
			CaseReference = context.CaseReference,
			ValidatedBy = context.ValidatedBy,
			ValidationDate = context.ValidationDate,
			LabProcedure = context.LabProcedure,
			Algorithm = computed.Algorithm,
			EvidencePath = (evidenceKind == null) ? Path.GetFullPath(evidencePath) : evidencePath,
			EvidenceKind = text,
			SetHash = setHash,
			ExpectedSource = expectedSource,
			ExpectedFormat = expected.FormatName,
			Note = expected.Note,
			MatchCount = matchCount,
			MismatchCount = num,
			MissingCount = num2,
			ExtraCount = num3,
			FileCount = list.Count,
			Verdict = WithSkippedNote(ResultLabels.Verdict(num, num2, num3), computed.Skipped.Count),
			Files = list,
			Skipped = computed.Skipped.Select(CopySkipped).ToList()
		};
		RecordIntegrity.Stamp(obj);
		return obj;
	}

	public static List<FileVerificationEntry> Compare(IReadOnlyList<FileHashEntry> computed, IReadOnlyList<ExpectedHashEntry> expected, bool ignorePathCase)
	{
		StringComparer comparer = (ignorePathCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		Dictionary<string, FileHashEntry> dictionary = new Dictionary<string, FileHashEntry>(comparer);
		foreach (FileHashEntry item3 in computed)
		{
			string text = PathKeys.Normalize(item3.Path);
			if (!dictionary.TryAdd(text, item3))
			{
				throw new InvalidDataException("Two evidence files normalize to '" + text + "'.");
			}
		}
		List<ExpectedHashEntry> list = expected.Where((ExpectedHashEntry entry) => entry.Path.Length == 0).ToList();
		if (list.Count > 1)
		{
			throw new InvalidDataException("The expected source contains more than one hash without a path.");
		}
		if (list.Count == 1)
		{
			if (expected.Count != 1)
			{
				throw new InvalidDataException("The expected source mixes a bare hash with paths.");
			}
			if (computed.Count != 1)
			{
				throw new InvalidDataException("A single hash without a path can only be compared with one evidence file.");
			}
			return new List<FileVerificationEntry> { CompareOne(computed[0], list[0]) };
		}
		foreach (ExpectedHashEntry item4 in expected)
		{
			PathKeys.Normalize(item4.Path);
		}
		HashSet<string> hashSet = new HashSet<string>(comparer);
		foreach (ExpectedHashEntry item5 in expected)
		{
			string text2 = PathKeys.Normalize(item5.Path);
			if (!hashSet.Add(text2))
			{
				throw new InvalidDataException("The expected list contains more than one hash for '" + text2 + "'.");
			}
		}
		Dictionary<string, List<FileHashEntry>> dictionary2 = computed.GroupBy<FileHashEntry, string>((FileHashEntry file) => PathKeys.FileName(PathKeys.Normalize(file.Path)), comparer).ToDictionary<IGrouping<string, FileHashEntry>, string, List<FileHashEntry>>((IGrouping<string, FileHashEntry> group) => group.Key, (IGrouping<string, FileHashEntry> group) => group.ToList(), comparer);
		Dictionary<string, List<ExpectedHashEntry>> dictionary3 = expected.GroupBy<ExpectedHashEntry, string>((ExpectedHashEntry entry) => PathKeys.FileName(PathKeys.Normalize(entry.Path)), comparer).ToDictionary<IGrouping<string, ExpectedHashEntry>, string, List<ExpectedHashEntry>>((IGrouping<string, ExpectedHashEntry> group) => group.Key, (IGrouping<string, ExpectedHashEntry> group) => group.ToList(), comparer);
		HashSet<string> hashSet2 = new HashSet<string>(comparer);
		List<FileVerificationEntry> list2 = new List<FileVerificationEntry>();
		foreach (ExpectedHashEntry item6 in expected)
		{
			string text3 = PathKeys.Normalize(item6.Path);
			FileHashEntry fileHashEntry = null;
			List<ExpectedHashEntry> value2;
			List<FileHashEntry> value3;
			if (dictionary.TryGetValue(text3, out var value))
			{
				fileHashEntry = value;
			}
			else if (AllowFileNameFallback(text3) && dictionary3.TryGetValue(PathKeys.FileName(text3), out value2) && value2.Count == 1 && dictionary2.TryGetValue(PathKeys.FileName(text3), out value3) && value3.Count == 1)
			{
				fileHashEntry = value3[0];
			}
			if (fileHashEntry == null)
			{
				list2.Add(new FileVerificationEntry
				{
					Path = text3,
					ExpectedSize = item6.Size,
					ExpectedHash = HashAlgorithms.NormalizeHex(item6.Hash),
					Result = "missing"
				});
			}
			else
			{
				string item = PathKeys.Normalize(fileHashEntry.Path);
				if (!hashSet2.Add(item))
				{
					throw new InvalidDataException("More than one expected path matches '" + fileHashEntry.Path + "'.");
				}
				list2.Add(CompareOne(fileHashEntry, item6));
			}
		}
		foreach (FileHashEntry item7 in computed)
		{
			string item2 = PathKeys.Normalize(item7.Path);
			if (!hashSet2.Contains(item2))
			{
				list2.Add(new FileVerificationEntry
				{
					Path = item7.Path,
					Size = item7.Size,
					ComputedHash = HashAlgorithms.NormalizeHex(item7.Hash),
					Result = "extra"
				});
			}
		}
		list2.Sort(delegate(FileVerificationEntry left, FileVerificationEntry right)
		{
			int num = ResultLabels.Rank(left.Result).CompareTo(ResultLabels.Rank(right.Result));
			return (num == 0) ? comparer.Compare(left.Path, right.Path) : num;
		});
		return list2;
	}

	private static bool AllowFileNameFallback(string expectedPath)
	{
		if (expectedPath.Contains(':') || expectedPath.StartsWith('/'))
		{
			return true;
		}
		return !expectedPath.Contains('/');
	}

	private static FileVerificationEntry CompareOne(FileHashEntry file, ExpectedHashEntry expected)
	{
		string text = HashAlgorithms.NormalizeHex(file.Hash);
		string text2 = HashAlgorithms.NormalizeHex(expected.Hash);
		bool flag = HashAlgorithms.FixedTimeEquals(text, text2);
		return new FileVerificationEntry
		{
			Path = file.Path,
			Size = file.Size,
			ExpectedSize = expected.Size,
			ComputedHash = text,
			ExpectedHash = text2,
			Result = (flag ? "match" : "mismatch")
		};
	}

	private static string WithSkippedNote(string verdict, int skipped)
	{
		if (skipped == 0)
		{
			return verdict;
		}
		return verdict + " " + ReportCopy.SkippedSummary(skipped);
	}

	private static SkippedEntry CopySkipped(SkippedEntry entry)
	{
		return new SkippedEntry
		{
			Path = entry.Path,
			Reason = entry.Reason
		};
	}
}
