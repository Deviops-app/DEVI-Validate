using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.SelfTest;

public static class SelfTestRunner
{
	public static async Task<SelfTestRecord> RunAsync(ExaminationContext context, CancellationToken cancellationToken)
	{
		List<SelfTestCheck> checks = new List<SelfTestCheck>();
		foreach (PublishedVectors.Vector item in PublishedVectors.All)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string text = HashText(item.Kind, item.Text);
			checks.Add(new SelfTestCheck
			{
				Name = item.Algorithm,
				Input = item.Input,
				Expected = item.Expected,
				Computed = text,
				Passed = string.Equals(text, item.Expected, StringComparison.Ordinal)
			});
		}
		List<SelfTestCheck> list = checks;
		list.Add(await ReadOnlyCheckAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
		int num = checks.Count((SelfTestCheck check) => !check.Passed);
		SelfTestRecord obj = new SelfTestRecord
		{
			BuildHash = BuildIdentity.TryCoreHash(),
			OperatingSystem = RuntimeInformation.OSDescription,
			TestedAt = context.Timestamp,
			TimeZone = context.TimeZoneId,
			MachineName = context.MachineName,
			ValidatedBy = context.ValidatedBy,
			ValidationDate = context.ValidationDate,
			LabProcedure = context.LabProcedure,
			PassedCount = checks.Count - num,
			FailedCount = num,
			Result = ((num == 0) ? "All self-test checks passed." : "One or more self-test checks did not pass."),
			Checks = checks
		};
		RecordIntegrity.Stamp(obj);
		return obj;
	}

	public static string HashText(HashAlgorithmKind kind, string text)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithms.ToName(kind));
		incrementalHash.AppendData(bytes);
		return Convert.ToHexString(incrementalHash.GetHashAndReset()).ToLowerInvariant();
	}

	private static async Task<SelfTestCheck> ReadOnlyCheckAsync(CancellationToken cancellationToken)
	{
		DirectoryInfo directory = Directory.CreateTempSubdirectory("devi-validate-selftest-");
		try
		{
			string file = Path.Combine(directory.FullName, "sample.txt");
			byte[] payload = "abc"u8.ToArray();
			File.WriteAllBytes(file, payload);
			DateTime dateTime = new DateTime(2020, 6, 15, 12, 0, 0, DateTimeKind.Utc);
			File.SetLastWriteTimeUtc(file, dateTime);
			File.SetLastAccessTimeUtc(file, dateTime);
			DateTime writeBefore = File.GetLastWriteTimeUtc(file);
			DateTime accessBefore = File.GetLastAccessTimeUtc(file);
			await EvidenceHasher.HashAsync(file, HashAlgorithmKind.Sha256, ExaminationContext.Capture(null, null), null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(file);
			DateTime lastAccessTimeUtc = File.GetLastAccessTimeUtc(file);
			bool flag = File.ReadAllBytes(file).AsSpan().SequenceEqual(payload);
			bool flag2 = writeBefore == lastWriteTimeUtc && accessBefore == lastAccessTimeUtc && flag;
			return new SelfTestCheck
			{
				Name = "Read-only",
				Input = "synthetic file",
				Expected = "content and timestamps unchanged",
				Computed = (flag2 ? "unchanged" : "content or timestamps changed"),
				Passed = flag2
			};
		}
		finally
		{
			try
			{
				directory.Delete(recursive: true);
			}
			catch (IOException)
			{
			}
		}
	}
}
