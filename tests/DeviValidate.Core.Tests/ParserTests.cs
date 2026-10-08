using DeviValidate.Core.Expected;
using DeviValidate.Core.Expected.Vendor;
using DeviValidate.Core.Hashing;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Tests;

public class ParserTests
{
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Fact]
    public void ReadsGnuAndBsdSumLines()
    {
        var gnu = ExpectedHashReader.ReadText($"{AbcSha256}  notes.txt\n", "SHA256SUMS", null);
        Assert.Equal(SumFileParser.GnuName, gnu.FormatName);
        Assert.Equal("notes.txt", gnu.Entries[0].Path);
        Assert.Equal(HashAlgorithmKind.Sha256, gnu.Algorithm);

        var binary = ExpectedHashReader.ReadText($"{AbcSha256} *photos/one.bin\n", "sums.txt", null);
        Assert.Equal("photos/one.bin", binary.Entries[0].Path);

        var bsd = ExpectedHashReader.ReadText($"SHA256 (notes.txt) = {AbcSha256}\n", "sums.txt", null);
        Assert.Equal(SumFileParser.BsdName, bsd.FormatName);
        Assert.Equal("notes.txt", bsd.Entries[0].Path);
    }

    [Fact]
    public void ReadsBareAndLabeledHash()
    {
        var bare = ExpectedHashReader.ReadText(AbcSha256 + "\n", "image.sha256", null);
        Assert.Equal(SumFileParser.BareName, bare.FormatName);
        Assert.Equal("", bare.Entries[0].Path);

        var labeled = ExpectedHashReader.ReadText("MD5: 900150983cd24fb0d6963f7d28e17f72\n", "image.md5", null);
        Assert.Equal(HashAlgorithmKind.Md5, labeled.Algorithm);
    }

    [Fact]
    public void ReadsCsvAndMappedTsv()
    {
        var csv = """
            path,sha256,size
            "notes, one.txt",ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad,3
            """;
        var parsed = ExpectedHashReader.ReadText(csv, "hashes.csv", null);
        Assert.Equal("CSV", parsed.FormatName);
        Assert.Equal("notes, one.txt", parsed.Entries[0].Path);
        Assert.Equal(3, parsed.Entries[0].Size);

        var tsv = "Item\tDigest\nnotes.txt\t" + AbcSha256 + "\n";
        var mapped = ExpectedHashReader.ReadText(tsv, "custom.tsv", new ExpectedReadOptions
        {
            PathColumn = "1",
            HashColumn = "Digest",
        });
        Assert.Equal("notes.txt", mapped.Entries[0].Path);
        Assert.Equal(HashAlgorithmKind.Sha256, mapped.Algorithm);
    }

    [Fact]
    public void ReportsHeadersWhenColumnsAreNotRecognized()
    {
        var csv = "alpha,beta\nnotes.txt," + AbcSha256 + "\n";
        var error = Assert.Throws<InvalidDataException>(() => ExpectedHashReader.ReadText(csv, "hashes.csv", null));
        Assert.Contains("alpha", error.Message, StringComparison.Ordinal);
        Assert.Contains("--path-column", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadsStampedManifestAndRejectsAnEditedOne()
    {
        var manifest = SampleManifest();
        var json = CanonicalJson.ToPretty(manifest);
        var expected = DeviManifestParser.Parse(json, requested: null);
        Assert.Equal(DeviManifestParser.ManifestFormat, expected.FormatName);
        Assert.Equal("notes.txt", expected.Entries[0].Path);

        var edited = json.Replace("notes.txt", "other.txt", StringComparison.Ordinal);
        var error = Assert.Throws<InvalidDataException>(() => DeviManifestParser.Parse(edited, null));
        Assert.Contains("integrity hash", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadsFtkImagerLogAndSelectsSha256WhenPresent()
    {
        var log = """
            Created By AccessData FTK Imager 4.7.1.4

            Image Information:
             Segment list:
              C:\Synthetic\phone.dd

            Image Verification Results:
             MD5 checksum:    900150983cd24fb0d6963f7d28e17f72 : verified
             SHA1 checksum:   a9993e364706816aba3e25717850c26c9cd0d89d : verified
             SHA256 checksum: ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad : verified
            """;
        var parsed = ExpectedHashReader.ReadText(log, "ftk.txt", null);
        Assert.Equal(FtkImagerLogParser.FormatName, parsed.FormatName);
        Assert.Equal(HashAlgorithmKind.Sha256, parsed.Algorithm);
        var entry = Assert.Single(parsed.Entries);
        Assert.Equal("C:/Synthetic/phone.dd", entry.Path);
        Assert.Contains("raw image", parsed.Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FtkLogRefusesContainerAndMultipleSegments()
    {
        var log = """
            FTK Imager
            Segment list:
              C:\Synthetic\phone.E01
            MD5 checksum: 900150983cd24fb0d6963f7d28e17f72 : verified
            """;
        var parsed = FtkImagerLogParser.Parse(log, HashAlgorithmKind.Md5);
        var error = Assert.Throws<InvalidOperationException>(() => ExpectedHashRules.EnsureCompatible("phone.E01", parsed));
        Assert.Contains("E01", error.Message, StringComparison.Ordinal);

        var split = """
            FTK Imager
            Image Verification Results:
            Segment list:
              D:\Images\disk.001
              D:\Images\disk.002
            MD5 checksum: 900150983cd24fb0d6963f7d28e17f72 : verified
            """;
        var splitError = Assert.Throws<InvalidDataException>(() => FtkImagerLogParser.Parse(split, null));
        Assert.Contains("more than one image segment", splitError.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VendorRegistryIsAnExtensionPoint()
    {
        VendorParserRegistry.Register(new ExampleVendorParser());
        var parsed = ExpectedHashReader.ReadText(
            "EXAMPLE-VENDOR-HASH-REPORT\nnotes.txt " + AbcSha256,
            "vendor.txt",
            null);
        Assert.Equal("Example Vendor", parsed.FormatName);
        Assert.Equal("notes.txt", parsed.Entries[0].Path);
    }

    private static HashManifest SampleManifest()
    {
        var manifest = new HashManifest
        {
            CreatedAt = new DateTimeOffset(2026, 10, 4, 2, 0, 0, TimeSpan.Zero),
            TimeZone = "UTC",
            MachineName = "test",
            Algorithm = "SHA-256",
            SourcePath = "/synthetic/evidence",
            Files = new List<FileHashEntry>
            {
                new()
                {
                    Path = "notes.txt",
                    Size = 3,
                    Hash = AbcSha256,
                },
            },
        };
        RecordIntegrity.Stamp(manifest);
        return manifest;
    }

    private sealed class ExampleVendorParser : IVendorReportParser
    {
        public string Name => "Example Vendor";

        public bool CanParse(string content, string? fileName)
        {
            return content.Contains("EXAMPLE-VENDOR-HASH-REPORT", StringComparison.Ordinal);
        }

        public ExpectedHashes Parse(string content, HashAlgorithmKind? algorithm)
        {
            return ExpectedHashSelection.Strict(
                Name,
                note: null,
                new[]
                {
                    new ExpectedHashEntry
                    {
                        Path = "notes.txt",
                        Hash = AbcSha256,
                        Algorithm = HashAlgorithmKind.Sha256,
                    },
                },
                algorithm);
        }
    }
}
