using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeviValidate.Cli;
using DeviValidate.Core;
using DeviValidate.Core.Expected;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.IO;
using DeviValidate.Core.Reporting;
using DeviValidate.Core.SelfTest;
using DeviValidate.Core.Verification;

namespace DeviValidate.Core.Tests;

public class ExaminerSuiteTests
{
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Fact]
    public async Task ComputedHashesMatchIndependentTools()
    {
        using var dir = new TempDir();
        var samples = new List<(string Name, byte[] Bytes)>
        {
            ("empty.bin", Array.Empty<byte>()),
            ("one.bin", new byte[] { 0x61 }),
            ("block64.bin", new byte[64]),
            ("block4096.bin", new byte[4096]),
            ("mib.bin", new byte[1024 * 1024]),
            ("cafe notes.txt", Encoding.UTF8.GetBytes("café")),
        };
        RandomNumberGenerator.Fill(samples[2].Bytes);
        RandomNumberGenerator.Fill(samples[3].Bytes);
        samples[3].Bytes[4095] = 0x5a;
        RandomNumberGenerator.Fill(samples[4].Bytes);
        samples[4].Bytes[0] = 1;
        samples[4].Bytes[^1] = 2;

        foreach (var sample in samples)
        {
            var path = dir.WriteBytes(sample.Name, sample.Bytes);
            foreach (var algorithm in new[] { HashAlgorithmKind.Sha256, HashAlgorithmKind.Sha1, HashAlgorithmKind.Md5 })
            {
                var manifest = await HashOne(path, algorithm);
                var computed = Assert.Single(manifest.Files).Hash;
                var external = ExternalHash(path, algorithm);
                Assert.Equal(external, computed);
            }
        }
    }

    [Fact]
    public void ParsersRejectMalformedHashesAndPathTricks()
    {
        var bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble()) + AbcSha256.ToUpperInvariant() + "  notes.txt\r\n";
        var parsed = ExpectedHashReader.ReadText(bom, "SHA256SUMS", null);
        Assert.Equal("notes.txt", parsed.Entries[0].Path);
        Assert.Equal(AbcSha256, parsed.Entries[0].Hash);

        var trailing = AbcSha256 + "  spaced name.txt   \n";
        var spaced = ExpectedHashReader.ReadText(trailing, "sums.txt", null);
        Assert.Equal("spaced name.txt", spaced.Entries[0].Path);

        var shortHash = new string('a', 63) + "  notes.txt\n";
        Assert.Throws<InvalidDataException>(() => ExpectedHashReader.ReadText(shortHash, "sums.txt", null));

        var climb = AbcSha256 + "  ../outside.txt\n";
        var climbError = Assert.Throws<InvalidDataException>(() => ExpectedHashReader.ReadText(climb, "sums.txt", null));
        Assert.Contains("..", climbError.Message, StringComparison.Ordinal);

        var duplicate = AbcSha256 + "  notes.txt\n" + new string('b', 64) + "  notes.txt\n";
        var expected = ExpectedHashReader.ReadText(duplicate, "sums.txt", null);
        var computed = new List<FileHashEntry>
        {
            new() { Path = "notes.txt", Size = 3, Hash = AbcSha256 },
        };
        var duplicateError = Assert.Throws<InvalidDataException>(() => Verifier.Compare(computed, expected.Entries, ignorePathCase: false));
        Assert.Contains("more than one hash", duplicateError.Message, StringComparison.Ordinal);

        var collision = new List<FileHashEntry>
        {
            new() { Path = "Notes.TXT", Size = 1, Hash = AbcSha256 },
            new() { Path = "notes.txt", Size = 1, Hash = AbcSha256 },
        };
        var collisionError = Assert.Throws<InvalidDataException>(() => Verifier.Compare(collision, expected.Entries.Take(1).ToList(), ignorePathCase: true));
        Assert.Contains("normalize", collisionError.Message, StringComparison.Ordinal);

        var brokenCsv = "\"unterminated,abc\n";
        Assert.ThrowsAny<Exception>(() => ExpectedHashReader.ReadText(brokenCsv, "hashes.csv", null));

        var badJson = "{ \"tool\": \"nope\" }";
        Assert.Throws<InvalidDataException>(() => ExpectedHashReader.ReadText(badJson, "record.json", null));
    }

    [Fact]
    public async Task MatchMismatchMissingAndExtraAreDistinct()
    {
        using var dir = new TempDir();
        var evidence = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "keep.txt"), "abc");
        File.WriteAllText(Path.Combine(evidence, "changed.txt"), "zzz");
        File.WriteAllText(Path.Combine(evidence, "extra.txt"), "qqq");
        var sum = AbcSha256 + "  keep.txt\n"
            + new string('a', 64) + "  changed.txt\n"
            + new string('b', 64) + "  missing.txt\n";
        var expected = ExpectedHashReader.ReadText(sum, "sums.txt", null);
        var manifest = await HashOne(evidence, HashAlgorithmKind.Sha256);
        var record = Verifier.Verify(manifest, expected, ExaminationContext.Capture(null, null), evidence, "sums.txt", false);
        Assert.Equal(1, record.MatchCount);
        Assert.Equal(1, record.MismatchCount);
        Assert.Equal(1, record.MissingCount);
        Assert.Equal(1, record.ExtraCount);
        Assert.Contains(record.Files, file => file.Result == ResultLabels.Match && file.Path == "keep.txt");
        Assert.Contains(record.Files, file => file.Result == ResultLabels.Mismatch && file.Path == "changed.txt");
        Assert.Contains(record.Files, file => file.Result == ResultLabels.Missing && file.Path == "missing.txt");
        Assert.Contains(record.Files, file => file.Result == ResultLabels.Extra && file.Path == "extra.txt");
    }

    [Fact]
    public async Task ReadOnlyHashLeavesBytesTimesAndAttributesAlone()
    {
        using var dir = new TempDir();
        var path = dir.Write("evidence.bin", "abc");
        var past = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, past);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        var beforeHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        var beforeWrite = File.GetLastWriteTimeUtc(path);
        var beforeAccess = File.GetLastAccessTimeUtc(path);
        var beforeAttributes = File.GetAttributes(path);

        try
        {
            var manifest = await HashOne(path, HashAlgorithmKind.Sha256);
            Assert.Equal(beforeHash, manifest.Files[0].Hash);
            Assert.Equal(beforeWrite, File.GetLastWriteTimeUtc(path));
            Assert.Equal(beforeAccess, File.GetLastAccessTimeUtc(path));
            Assert.Equal(beforeAttributes, File.GetAttributes(path));
            Assert.True(beforeAttributes.HasFlag(FileAttributes.ReadOnly));
            Assert.Equal("abc", File.ReadAllText(path));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [Fact]
    public void RelativeReportPathInsideEvidenceIsRefused()
    {
        using var dir = new TempDir();
        var evidence = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(evidence);
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(evidence);
            var error = Assert.Throws<InvalidOperationException>(() => OutputPathGuard.EnsureOutside(evidence, "record.html"));
            Assert.Equal(OutputPathGuard.Refusal, error.Message);
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }

    [Fact]
    public async Task AFileThatDisappearsOrChangesIsReported()
    {
        using var dir = new TempDir();
        var gone = dir.Write("gone.bin", "abc");
        var changed = dir.Write("changed.bin", "abc");
        try
        {
            EvidenceHasher.BeforeOpen = path =>
            {
                if (path.EndsWith("gone.bin", StringComparison.Ordinal))
                {
                    File.Delete(path);
                }

                if (path.EndsWith("changed.bin", StringComparison.Ordinal))
                {
                    File.WriteAllText(path, "different");
                    File.SetLastWriteTimeUtc(path, new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc));
                }
            };

            var manifest = await HashOne(dir.Path, HashAlgorithmKind.Sha256);
            Assert.Empty(manifest.Files);
            Assert.Contains(manifest.Skipped, entry => entry.Path == "gone.bin" && entry.Reason.Contains("could not be read", StringComparison.Ordinal));
            Assert.Contains(manifest.Skipped, entry => entry.Path == "changed.bin" && entry.Reason.Contains("changed while it was being read", StringComparison.Ordinal));
            var html = HtmlReport.Render(manifest);
            Assert.Contains("gone.bin", html, StringComparison.Ordinal);
            Assert.Contains("could not be read", html, StringComparison.Ordinal);
            Assert.Contains("changed while it was being read", html, StringComparison.Ordinal);
            Assert.DoesNotContain("symbolic links", html, StringComparison.Ordinal);
        }
        finally
        {
            EvidenceHasher.BeforeOpen = null;
        }
    }

    [Fact]
    public async Task CancelDuringAReadDoesNotChangeTheFile()
    {
        using var dir = new TempDir();
        var path = dir.WriteBytes("big.bin", new byte[2 * 1024 * 1024]);
        var before = SHA256.HashData(File.ReadAllBytes(path));
        var write = File.GetLastWriteTimeUtc(path);
        using var cancel = new CancellationTokenSource();
        var progress = new Progress<HashProgress>(_ => cancel.Cancel());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            EvidenceHasher.HashAsync(path, HashAlgorithmKind.Sha256, ExaminationContext.Capture(null, null), progress, cancel.Token));
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(path)));
        Assert.Equal(write, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public async Task RecordsRoundTripForEachResultKind()
    {
        using var dir = new TempDir();
        var evidence = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(Path.Combine(evidence, "photos"));
        File.WriteAllText(Path.Combine(evidence, "café.txt"), "abc");
        var manifest = await HashOne(evidence, HashAlgorithmKind.Sha256);
        var expected = ExpectedHashReader.ReadText(AbcSha256 + "  missing.txt\n" + new string('c', 64) + "  café.txt\n", "sums.txt", null);
        var record = Verifier.Verify(manifest, expected, ExaminationContext.Capture(null, "SYNTH-001"), evidence, "sums.txt", false);
        var reports = Path.Combine(dir.Path, "reports");
        var written = RecordStore.WriteVerification(evidence, reports, record);
        var json = File.ReadAllText(written.JsonPath!);
        var loaded = CanonicalJson.FromJson<VerificationRecord>(json);
        Assert.True(RecordIntegrity.Matches(loaded));
        Assert.Equal(record.Verdict, loaded.Verdict);
        var html = File.ReadAllText(written.HtmlPath!);
        Assert.Contains(record.Verdict, html, StringComparison.Ordinal);
        Assert.Contains("caf&#233;.<wbr>txt", html, StringComparison.Ordinal);
        Assert.Contains(loaded.Files, file => file.Path == "café.txt");
        var pdf = File.ReadAllBytes(written.PdfPath!);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.Contains(record.Verdict, Encoding.Latin1.GetString(pdf), StringComparison.Ordinal);
        AssertSchema(json);
    }

    [Fact]
    public async Task CliExitCodesMatchTheVerdict()
    {
        using var dir = new TempDir();
        var evidenceDir = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(evidenceDir);
        var evidence = Path.Combine(evidenceDir, "notes.txt");
        File.WriteAllText(evidence, "abc");
        var reports = Path.Combine(dir.Path, "reports");
        Directory.CreateDirectory(reports);
        Assert.Equal(0, await Run("verify", evidence, "--hash", AbcSha256, "--output-dir", reports));
        Assert.Equal(2, await Run("verify", evidence, "--hash", new string('a', 64), "--output-dir", reports));
        Assert.Equal(1, await Run("verify", evidence));
        var json = Directory.GetFiles(reports, "*.json")[0];
        Assert.Equal(0, await Run("check", json));
        var broken = File.ReadAllText(json).Replace("notes.txt", "other.txt", StringComparison.Ordinal);
        var brokenPath = Path.Combine(dir.Path, "broken.json");
        File.WriteAllText(brokenPath, broken);
        Assert.Equal(2, await Run("check", brokenPath));
    }

    [Fact]
    public void SetHashIgnoresInputOrder()
    {
        var first = new string('a', 64);
        var forward = SetHash.Compute(new[] { ("b.txt", AbcSha256), ("a.txt", first) }, HashAlgorithmKind.Sha256);
        var reverse = SetHash.Compute(new[] { ("a.txt", first), ("b.txt", AbcSha256) }, HashAlgorithmKind.Sha256);
        Assert.Equal(forward, reverse);
        Assert.Equal(64, forward.Length);
        Assert.NotEqual(forward, SetHash.Compute(new[] { ("a.txt", AbcSha256), ("b.txt", first) }, HashAlgorithmKind.Sha256));
    }

    [Fact]
    public async Task LongPathAndManyFilesStillHash()
    {
        using var dir = new TempDir();
        var leaf = dir.Path;
        for (var depth = 0; depth < 4; depth++)
        {
            leaf = Path.Combine(leaf, new string('p', 40) + depth.ToString());
        }

        Directory.CreateDirectory(leaf);
        if (leaf.Length <= 260)
        {
            leaf = Path.Combine(leaf, new string('q', 80));
            Directory.CreateDirectory(leaf);
        }

        var file = Path.Combine(leaf, "notes.txt");
        File.WriteAllText(file, "abc");
        Assert.True(file.Length > 260);
        var manifest = await HashOne(file, HashAlgorithmKind.Sha256);
        Assert.Equal(AbcSha256, Assert.Single(manifest.Files).Hash);

        var many = Path.Combine(dir.Path, "many");
        Directory.CreateDirectory(many);
        const int count = 100_000;
        for (var index = 0; index < count; index++)
        {
            File.WriteAllText(Path.Combine(many, index.ToString("D6") + ".txt"), index % 2 == 0 ? "a" : "b");
        }

        var started = Stopwatch.StartNew();
        var folder = await HashOne(many, HashAlgorithmKind.Sha256);
        started.Stop();
        Assert.Equal(count, folder.Files.Count);
        Assert.True(started.Elapsed < TimeSpan.FromMinutes(3), "100000 files took " + started.Elapsed);
    }

    private static void AssertSchema(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        foreach (var name in new[] { "schemaVersion", "kind", "tool", "version", "algorithm", "verdict", "files", "integrity" })
        {
            Assert.True(root.TryGetProperty(name, out _), name);
        }

        Assert.Equal("verification-record", root.GetProperty("kind").GetString());
        Assert.Equal(64, root.GetProperty("integrity").GetProperty("hash").GetString()!.Length);
        foreach (var file in root.GetProperty("files").EnumerateArray())
        {
            var result = file.GetProperty("result").GetString();
            Assert.Contains(result, new[] { "match", "mismatch", "missing", "extra" });
        }
    }

    private static async Task<int> Run(params string[] args)
    {
        var stdout = Console.Out;
        var stderr = Console.Error;
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
            return await Program.Main(args);
        }
        finally
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
        }
    }

    private static string ExternalHash(string path, HashAlgorithmKind algorithm)
    {
        var name = algorithm switch
        {
            HashAlgorithmKind.Sha256 => "sha256",
            HashAlgorithmKind.Sha1 => "sha1",
            _ => "md5",
        };
        if (OperatingSystem.IsWindows())
        {
            var fileHash = WindowsFileHash(path, name);
            if (new FileInfo(path).Length == 0)
            {
                var empty = PublishedVectors.All.Single(vector => vector.Kind == algorithm && vector.Text.Length == 0).Expected;
                Assert.Equal(empty, fileHash);
                return fileHash;
            }

            var certutil = RunCapture("certutil", "-hashfile", path, name.ToUpperInvariant());
            var line = certutil.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .First(item => item.Length >= 32 && item.All(ch => Uri.IsHexDigit(ch) || ch == ' '));
            var certutilHash = line.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
            Assert.Equal(certutilHash, fileHash);
            return certutilHash;
        }

        var sum = RunCapture(name + "sum", path).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var openssl = RunCapture("openssl", "dgst", "-" + name, path);
        var opensslHash = openssl.Split(' ').Last().Trim().ToLowerInvariant();
        var python = RunCapture(
            "python3",
            "-c",
            "import hashlib,sys; print(hashlib." + name + "(open(sys.argv[1],'rb').read()).hexdigest())",
            path).Trim();
        Assert.Equal(sum, opensslHash);
        Assert.Equal(sum, python);
        return sum;
    }

    private static string WindowsFileHash(string path, string name)
    {
        var literal = path.Replace("'", "''", StringComparison.Ordinal);
        return RunCapture(
            "powershell",
            "-NoProfile",
            "-Command",
            "(Get-FileHash -LiteralPath '" + literal + "' -Algorithm " + name.ToUpperInvariant() + ").Hash").Trim().ToLowerInvariant();
    }

    private static string RunCapture(string file, params string[] args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = file,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, file + " failed: " + stderr);
        return stdout;
    }

    private static Task<HashManifest> HashOne(string path, HashAlgorithmKind algorithm)
    {
        return EvidenceHasher.HashAsync(path, algorithm, ExaminationContext.Capture(null, null), null, CancellationToken.None);
    }
}
