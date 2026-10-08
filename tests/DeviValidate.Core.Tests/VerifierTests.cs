using DeviValidate.Core.Expected;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.Verification;

namespace DeviValidate.Core.Tests;

public class VerifierTests
{
    private const string Abc = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    private const string Other = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [Fact]
    public void ClassifiesMatchMismatchMissingAndExtra()
    {
        var computed = Manifest(
            File("notes.txt", 3, Abc),
            File("photos/one.bin", 4, Other),
            File("extra.log", 1, Abc));
        var expected = Expect(
            Entry("notes.txt", Abc),
            Entry("photos/one.bin", Abc),
            Entry("missing.txt", Abc));

        var record = Verify(computed, expected);
        Assert.Equal(1, record.MatchCount);
        Assert.Equal(1, record.MismatchCount);
        Assert.Equal(1, record.MissingCount);
        Assert.Equal(1, record.ExtraCount);
        Assert.Equal(ResultLabels.MismatchAndMissingVerdict, record.Verdict);
        Assert.Equal(ResultLabels.Mismatch, record.Files[0].Result);
        Assert.Equal("photos/one.bin", record.Files[0].Path);
    }

    [Theory]
    [InlineData(0, 0, 0, ResultLabels.AllMatchVerdict)]
    [InlineData(0, 0, 2, ResultLabels.ExtrasVerdict)]
    [InlineData(1, 0, 0, ResultLabels.MismatchVerdict)]
    [InlineData(0, 3, 0, ResultLabels.MissingVerdict)]
    [InlineData(1, 1, 4, ResultLabels.MismatchAndMissingVerdict)]
    public void VerdictWordingIsFactual(int mismatch, int missing, int extra, string expected)
    {
        Assert.Equal(expected, ResultLabels.Verdict(mismatch, missing, extra));
    }

    [Fact]
    public void MatchesUniqueFileNameFromAnAbsoluteExpectedPath()
    {
        var computed = Manifest(File("phone.dd", 3, Abc));
        var expected = Expect(Entry("C:/Synthetic/phone.dd", Abc));
        var record = Verify(computed, expected);
        Assert.Equal(1, record.MatchCount);
        Assert.Equal("phone.dd", record.Files[0].Path);
    }

    [Fact]
    public void DoesNotPairDifferentDirectoriesByFileName()
    {
        var computed = Manifest(File("b/notes.txt", 3, Abc));
        var expected = Expect(Entry("a/notes.txt", Abc));
        var record = Verify(computed, expected);
        Assert.Equal(1, record.MissingCount);
        Assert.Equal(1, record.ExtraCount);
        Assert.Equal(0, record.MatchCount);
    }

    [Fact]
    public void BareHashComparesWithTheSingleEvidenceFile()
    {
        var computed = Manifest(File("image.dd", 3, Abc));
        var expected = new ExpectedHashes
        {
            FormatName = "bare hash",
            Algorithm = HashAlgorithmKind.Sha256,
            Entries = new[]
            {
                new ExpectedHashEntry { Path = "", Hash = Abc, Algorithm = HashAlgorithmKind.Sha256 },
            },
        };
        var record = Verify(computed, expected);
        Assert.Equal(ResultLabels.Match, Assert.Single(record.Files).Result);
    }

    private static VerificationRecord Verify(HashManifest computed, ExpectedHashes expected)
    {
        return Verifier.Verify(
            computed,
            expected,
            ExaminationContext.Capture(null, null),
            "/synthetic/evidence",
            "synthetic",
            ignorePathCase: false);
    }

    private static HashManifest Manifest(params FileHashEntry[] files)
    {
        return new HashManifest
        {
            Algorithm = "SHA-256",
            SourcePath = "/synthetic/evidence",
            Files = files.ToList(),
        };
    }

    private static FileHashEntry File(string path, long size, string hash)
    {
        return new FileHashEntry { Path = path, Size = size, Hash = hash };
    }

    private static ExpectedHashes Expect(params ExpectedHashEntry[] entries)
    {
        return new ExpectedHashes
        {
            FormatName = "GNU sum",
            Algorithm = HashAlgorithmKind.Sha256,
            Entries = entries,
        };
    }

    private static ExpectedHashEntry Entry(string path, string hash)
    {
        return new ExpectedHashEntry { Path = path, Hash = hash, Algorithm = HashAlgorithmKind.Sha256 };
    }
}
