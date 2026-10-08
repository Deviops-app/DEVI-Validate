using System.Security.Cryptography;
using System.Text;
using DeviValidate.Core;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.Reporting;
using DeviValidate.Core.SelfTest;
using PdfSharp.Pdf.IO;

namespace DeviValidate.Core.Tests;

public class ExportAndSelfTestTests
{
    [Fact]
    public void SetHashIgnoresInputOrder()
    {
        const string notes = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        const string image = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        var forward = SetHash.Compute([("notes.txt", notes), ("image.bin", image)], HashAlgorithmKind.Sha256);
        var reverse = SetHash.Compute([("image.bin", image), ("notes.txt", notes)], HashAlgorithmKind.Sha256);
        Assert.Equal(forward, reverse);

        var canonical = "image.bin\t" + image + "\n" + "notes.txt\t" + notes + "\n";
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        Assert.Equal(expected, forward);

        var changed = SetHash.Compute([("notes.txt", image), ("image.bin", notes)], HashAlgorithmKind.Sha256);
        Assert.NotEqual(forward, changed);
    }

    [Fact]
    public void ExportNamesDoNotOverwrite()
    {
        using var dir = new TempDir();
        var stem = new string('a', 64);
        File.WriteAllText(Path.Combine(dir.Path, stem + ".pdf"), "keep");
        var paths = ExportNames.UniqueSet(dir.Path, stem, ".html", ".json", ".pdf");
        Assert.Equal(stem + "_2.html", Path.GetFileName(paths[0]));
        Assert.Equal(stem + "_2.json", Path.GetFileName(paths[1]));
        Assert.Equal(stem + "_2.pdf", Path.GetFileName(paths[2]));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(dir.Path, stem + ".pdf")));

        var again = ExportNames.Unused(Path.Combine(dir.Path, stem + ".pdf"));
        Assert.Equal(stem + "_2.pdf", Path.GetFileName(again));
    }

    [Fact]
    public void SingleFileExportUsesTheComputedHash()
    {
        using var dir = new TempDir();
        var evidenceDir = Path.Combine(dir.Path, "evidence");
        Directory.CreateDirectory(evidenceDir);
        var evidence = Path.Combine(evidenceDir, "notes.txt");
        File.WriteAllText(evidence, "abc");
        var manifest = EvidenceHasher.HashAsync(
            evidence,
            HashAlgorithmKind.Sha256,
            ExaminationContext.Capture(null, null),
            null,
            CancellationToken.None).GetAwaiter().GetResult();
        var expected = DeviValidate.Core.VerificationWorkflow.SingleHash(evidence, manifest.Files[0].Hash, HashAlgorithmKind.Sha256);
        var record = DeviValidate.Core.Verification.Verifier.Verify(
            manifest,
            expected,
            ExaminationContext.Capture(null, null, "Lab bench", "2026-10-04", "SOP-1"),
            evidence,
            "pasted hash",
            ignorePathCase: false);
        Assert.Equal("file", record.EvidenceKind);
        Assert.Null(record.SetHash);
        Assert.Equal(manifest.Files[0].Hash, ExportNames.Stem(record));
        Assert.Equal("Lab bench", record.ValidatedBy);

        var reports = Path.Combine(dir.Path, "reports");
        var written = RecordStore.WriteVerification(evidence, reports, record);
        Assert.Equal(manifest.Files[0].Hash + ".pdf", Path.GetFileName(written.PdfPath));
        var html = File.ReadAllText(written.HtmlPath!);
        Assert.Contains("entered manually", html, StringComparison.Ordinal);
        Assert.Contains("Lab bench", html, StringComparison.Ordinal);
        Assert.Contains("SOP-1", html, StringComparison.Ordinal);
        Assert.Contains(ReportCopy.MatchMeans, html, StringComparison.Ordinal);
        Assert.Contains("What you do to verify", html, StringComparison.Ordinal);
        Assert.Contains("physical analyzer", html, StringComparison.Ordinal);
        Assert.Contains("M0 21.5L190.5 21.5", html, StringComparison.Ordinal);
        Assert.Contains("#0A0A0B", html, StringComparison.Ordinal);
        Assert.Contains(">Validate<", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"mark\"", html, StringComparison.Ordinal);
        Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, BrandAssets.WordmarkDarkPng.Take(4).ToArray());
        Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, BrandAssets.MarkDarkPng.Take(4).ToArray());
        Assert.Contains("#4B8DF8", html, StringComparison.Ordinal);
        Assert.DoesNotContain("draft", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\u2014", html, StringComparison.Ordinal);
        AssertPdf(written.PdfPath!, minimumPages: 2);
    }

    [Fact]
    public void SelfTestPassesPublishedVectorsAndWritesANamedPdf()
    {
        using var dir = new TempDir();
        var record = SelfTestRunner.RunAsync(
            ExaminationContext.Capture(null, null, validatedBy: null, validationDate: null, labProcedure: "bench check"),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert.Equal(0, record.FailedCount);
        Assert.Equal(ReportCopy.PassedResult, record.Result);
        Assert.Contains(record.Checks, check => check.Name == "SHA-256" && check.Input == "empty string" && check.Passed);
        Assert.Contains(record.Checks, check => check.Name == "MD5" && check.Input.Contains("abc", StringComparison.Ordinal) && check.Passed);
        Assert.Contains(record.Checks, check => check.Name == "Read-only" && check.Passed);
        Assert.Equal("bench check", record.LabProcedure);
        Assert.True(RecordIntegrity.Matches(record));

        var written = RecordStore.WriteSelfTest(null, dir.Path, record);
        var pdfName = Path.GetFileName(written.PdfPath);
        Assert.StartsWith("selftest_" + ToolInfo.Version + "_", pdfName, StringComparison.Ordinal);
        Assert.EndsWith(".pdf", pdfName, StringComparison.Ordinal);
        Assert.Equal(Path.GetFileNameWithoutExtension(written.PdfPath) + ".html", Path.GetFileName(written.HtmlPath));
        var html = File.ReadAllText(written.HtmlPath!);
        Assert.Contains("Tool validation record", html, StringComparison.Ordinal);
        Assert.Contains(ReportCopy.NotRecorded, html, StringComparison.Ordinal);
        Assert.Contains("bench check", html, StringComparison.Ordinal);
        Assert.DoesNotContain("draft", html, StringComparison.OrdinalIgnoreCase);
        AssertPdf(written.PdfPath!, minimumPages: 1);

        var loaded = CanonicalJson.FromJson<SelfTestRecord>(File.ReadAllText(written.JsonPath!));
        Assert.True(RecordIntegrity.Matches(loaded));
    }

    [Fact]
    public void ExpectedSourceLabelsUsePlainNames()
    {
        Assert.Equal("FTK Imager log", ExpectedSourceLabels.Describe("FTK Imager text log"));
        Assert.Equal("sum file", ExpectedSourceLabels.Describe("GNU sum"));
        Assert.Equal("entered manually", ExpectedSourceLabels.Describe("single hash"));
        Assert.Equal("DEVI Validate manifest", ExpectedSourceLabels.Describe("DEVI Validate manifest"));
    }

    private static void AssertPdf(string path, int minimumPages)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        using var stream = new MemoryStream(bytes);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.True(document.PageCount >= minimumPages);
    }
}
