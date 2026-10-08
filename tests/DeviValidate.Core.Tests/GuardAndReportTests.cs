using DeviValidate.Core.Hashing;
using DeviValidate.Core.IO;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Tests;

public class GuardAndReportTests
{
    [Fact]
    public void RefusesOutputInsideTheEvidenceFolder()
    {
        using var dir = new TempDir();
        var evidence = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(evidence);
        var sibling = Path.Combine(dir.Path, "reports", "record.html");
        OutputPathGuard.EnsureOutside(evidence, sibling);

        var inside = Path.Combine(evidence, "record.html");
        var error = Assert.Throws<InvalidOperationException>(() => OutputPathGuard.EnsureOutside(evidence, inside));
        Assert.Equal(OutputPathGuard.Refusal, error.Message);
        Assert.False(File.Exists(inside));
    }

    [SymlinkFact]
    public void RefusesASymlinkThatPointsInsideTheEvidenceFolder()
    {
        using var dir = new TempDir();
        var evidence = Path.Combine(dir.Path, "evidence");
        var nested = Path.Combine(evidence, "nested");
        Directory.CreateDirectory(nested);
        var link = Path.Combine(dir.Path, "reports-link");
        LinkSupport.CreateDirectorySymlink(link, nested);

        var error = Assert.Throws<InvalidOperationException>(() =>
            TextFiles.WriteOutside(evidence, Path.Combine(link, "record.html"), "nope"));
        Assert.Equal(OutputPathGuard.Refusal, error.Message);
        Assert.False(File.Exists(Path.Combine(nested, "record.html")));
    }

    [WindowsJunctionFact]
    public void RefusesAJunctionThatPointsInsideTheEvidenceFolder()
    {
        using var dir = new TempDir();
        var evidence = Path.Combine(dir.Path, "evidence");
        var nested = Path.Combine(evidence, "nested");
        Directory.CreateDirectory(nested);
        var link = Path.Combine(dir.Path, "reports-junction");
        LinkSupport.CreateJunction(link, nested);

        var error = Assert.Throws<InvalidOperationException>(() =>
            TextFiles.WriteOutside(evidence, Path.Combine(link, "record.html"), "nope"));
        Assert.Equal(OutputPathGuard.Refusal, error.Message);
        Assert.False(File.Exists(Path.Combine(nested, "record.html")));
    }

    [Fact]
    public void SingleFileEvidenceLocationIsItsFolder()
    {
        using var dir = new TempDir();
        var evidence = Path.Combine(dir.Path, "image.dd");
        File.WriteAllText(evidence, "abc");
        var beside = Path.Combine(dir.Path, "record.html");
        var error = Assert.Throws<InvalidOperationException>(() => OutputPathGuard.EnsureOutside(evidence, beside));
        Assert.Equal(OutputPathGuard.Refusal, error.Message);
    }

    [Fact]
    public void IntegrityHashSurvivesPrettyJsonAndFailsAfterAChange()
    {
        var manifest = new HashManifest
        {
            CreatedAt = new DateTimeOffset(2026, 10, 4, 12, 30, 0, TimeSpan.FromHours(-7)),
            TimeZone = "America/Los_Angeles",
            MachineName = "bench",
            Examiner = "Café bench",
            CaseReference = "SYNTH-001",
            Algorithm = "SHA-256",
            SourcePath = "/synthetic/evidence",
            Files = new List<FileHashEntry>
            {
                new() { Path = "notes.txt", Size = 3, Hash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad" },
            },
        };
        RecordIntegrity.Stamp(manifest);
        var pretty = CanonicalJson.ToPretty(manifest);
        var loaded = CanonicalJson.FromJson<HashManifest>(pretty);
        Assert.True(RecordIntegrity.Matches(loaded));

        loaded.Files[0].Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        Assert.False(RecordIntegrity.Matches(loaded));
    }

    [Fact]
    public void HtmlRecordContainsTheVerdictHashesAndEscapedText()
    {
        using var dir = new TempDir();
        var evidence = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "notes.txt"), "abc");
        File.WriteAllText(Path.Combine(evidence, "changed.txt"), "abc");
        var manifest = EvidenceHasher.HashAsync(
            evidence,
            HashAlgorithmKind.Sha256,
            ExaminationContext.Capture("A. Examiner <lab>", "SYNTH-001"),
            null,
            CancellationToken.None).GetAwaiter().GetResult();
        File.WriteAllText(Path.Combine(evidence, "changed.txt"), "zzz");
        var again = EvidenceHasher.HashAsync(
            evidence,
            HashAlgorithmKind.Sha256,
            ExaminationContext.Capture("A. Examiner <lab>", "SYNTH-001"),
            null,
            CancellationToken.None).GetAwaiter().GetResult();
        var expected = DeviValidate.Core.Expected.DeviManifestParser.Parse(CanonicalJson.ToPretty(manifest), null);
        var record = DeviValidate.Core.Verification.Verifier.Verify(
            again,
            expected,
            ExaminationContext.Capture("A. Examiner <lab>", "SYNTH-001"),
            evidence,
            "manifest.json",
            ignorePathCase: false);

        var output = Path.Combine(dir.Path, "reports");
        var written = RecordStore.WriteVerification(evidence, output, record);
        var html = File.ReadAllText(written.HtmlPath!);
        Assert.Contains(ResultLabels.MismatchVerdict, html, StringComparison.Ordinal);
        Assert.Contains("Mismatch", html, StringComparison.Ordinal);
        Assert.Contains(record.Integrity.Hash, html, StringComparison.Ordinal);
        Assert.Contains("A. Examiner &lt;lab&gt;", html, StringComparison.Ordinal);
        Assert.Contains("SYNTH-001", html, StringComparison.Ordinal);
        Assert.Contains(ReportCopy.Independent, html, StringComparison.Ordinal);
        Assert.Contains("DEVI Validate manifest", html, StringComparison.Ordinal);
        Assert.Contains(ReportCopy.NotRecorded, html, StringComparison.Ordinal);
        Assert.Contains(ReportCopy.RedMeans, html, StringComparison.Ordinal);
        Assert.DoesNotContain(ReportCopy.MatchMeans, html, StringComparison.Ordinal);
        Assert.Contains("does not show who changed the file", html, StringComparison.Ordinal);
        Assert.Contains("#9e1c1c", html, StringComparison.Ordinal);
        Assert.Contains("#4B8DF8", html, StringComparison.Ordinal);
        Assert.Contains("class=\"bad\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("draft", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\u2014", html, StringComparison.Ordinal);
        Assert.Equal("folder", record.EvidenceKind);
        Assert.False(string.IsNullOrWhiteSpace(record.SetHash));
        Assert.Equal(record.SetHash + ".html", Path.GetFileName(written.HtmlPath));
        Assert.Equal(record.SetHash + ".pdf", Path.GetFileName(written.PdfPath));
        var pdf = File.ReadAllBytes(written.PdfPath!);
        Assert.True(pdf.Length > 5);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.Equal(1, record.MismatchCount);
        Assert.Equal(1, record.MatchCount);
        Assert.True(RecordIntegrity.Matches(CanonicalJson.FromJson<VerificationRecord>(File.ReadAllText(written.JsonPath!))));
    }
}
