using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeviValidate.Core.IO;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Hashing;

/// <summary>Hashes a file or walks a folder. Symbolic links are not followed.</summary>
public static class EvidenceHasher
{
	private sealed class ByteProgress : IProgress<long>
	{
		private readonly Action<long> _report;

		public ByteProgress(Action<long> report)
		{
			_report = report;
		}

		public void Report(long value)
		{
			_report(value);
		}
	}

	/// <summary>Test seam. Production leaves this null. It runs after the scan and before each file is opened.</summary>
	private static readonly AsyncLocal<Action<string>?> BeforeOpenSlot = new AsyncLocal<Action<string>>();

	internal static Action<string>? BeforeOpen
	{
		get
		{
			return BeforeOpenSlot.Value;
		}
		set
		{
			BeforeOpenSlot.Value = value;
		}
	}

	public static async Task<HashManifest> HashAsync(string evidencePath, HashAlgorithmKind algorithm, ExaminationContext context, IProgress<HashProgress>? progress, CancellationToken cancellationToken)
	{
		IProgress<HashProgress> progress2 = progress;
		EvidenceScan scan = EvidenceTree.Scan(evidencePath);
		List<FileHashEntry> entries = new List<FileHashEntry>(scan.Files.Count);
		List<SkippedEntry> skipped = scan.Skipped.ToList();
		for (int index = 0; index < scan.Files.Count; index++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			EvidenceFile file = scan.Files[index];
			int fileIndex = index + 1;
			int fileCount = scan.Files.Count;
			progress2?.Report(new HashProgress(file.RelativePath, 0L, file.Length, fileIndex, fileCount));
			DateTime scannedWrite = File.GetLastWriteTimeUtc(file.FullPath);
			BeforeOpen?.Invoke(file.FullPath);
			try
			{
				await using FileStream stream = ReadOnlyFile.Open(file.FullPath);
				string hash = await StreamingHasher.HashAsync(stream, algorithm, (progress2 == null) ? null : new ByteProgress(delegate(long read)
				{
					progress2.Report(new HashProgress(file.RelativePath, read, file.Length, fileIndex, fileCount));
				}), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				long num = (stream.CanSeek ? stream.Position : file.Length);
				FileInfo fileInfo = new FileInfo(file.FullPath);
				if (num != file.Length || fileInfo.Length != file.Length || fileInfo.LastWriteTimeUtc != scannedWrite)
				{
					skipped.Add(new SkippedEntry
					{
						Path = file.RelativePath,
						Reason = "The file changed while it was being read."
					});
					continue;
				}
				entries.Add(new FileHashEntry
				{
					Path = file.RelativePath,
					Size = file.Length,
					Hash = hash
				});
			}
			catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
			{
				skipped.Add(new SkippedEntry
				{
					Path = file.RelativePath,
					Reason = "The file could not be read. " + ex.Message
				});
			}
		}
		HashManifest hashManifest = new HashManifest();
		hashManifest.CreatedAt = context.Timestamp;
		hashManifest.TimeZone = context.TimeZoneId;
		hashManifest.MachineName = context.MachineName;
		hashManifest.Examiner = context.Examiner;
		hashManifest.Agency = context.Agency;
		hashManifest.CaseReference = context.CaseReference;
		hashManifest.Algorithm = HashAlgorithms.DisplayName(algorithm);
		hashManifest.SourcePath = scan.RootPath;
		hashManifest.Files = entries;
		hashManifest.Skipped = skipped;
		hashManifest.Skipped.Sort((SkippedEntry left, SkippedEntry right) => string.CompareOrdinal(left.Path, right.Path));
		RecordIntegrity.Stamp(hashManifest);
		return hashManifest;
	}
}
