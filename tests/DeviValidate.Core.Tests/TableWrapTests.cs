using System.Text;
using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Tests;

public class TableWrapTests
{
    private const string Sha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Fact]
    public void PathsBreakOnSeparatorsAndSoftCharacters()
    {
        Assert.Equal(
            ["Documents/", "messages.sqlite"],
            TableWrap.WrapPath("Documents/messages.sqlite", 20, Length));
        Assert.Equal(
            ["DCIM/Camera/", "IMG_0412.jpg"],
            TableWrap.WrapPath("DCIM/Camera/IMG_0412.jpg", 12, Length));
        Assert.Equal(
            ["Exports/", "extraction-", "summary.pdf"],
            TableWrap.WrapPath("Exports/extraction-summary.pdf", 16, Length));
        Assert.Equal(
            ["Folder\\", "file_name.", "txt"],
            TableWrap.WrapPath("Folder\\file_name.txt", 10, Length));
        Assert.Equal(
            ["ABCDEF", "GHIJKL", "MNOP"],
            TableWrap.WrapPath("ABCDEFGHIJKLMNOP", 6, Length));

        var sqlite = TableWrap.WrapPath("Documents/messages.sqlite", 20, Length);
        Assert.DoesNotContain(sqlite, line => line.EndsWith("sqlit", StringComparison.Ordinal));
        var photo = TableWrap.WrapPath("DCIM/Camera/IMG_0412.jpg", 12, Length);
        Assert.DoesNotContain(photo, line => line.EndsWith("jp", StringComparison.Ordinal));
        var summary = TableWrap.WrapPath("Exports/extraction-summary.pdf", 16, Length);
        Assert.DoesNotContain(summary, line => line.EndsWith("summar", StringComparison.Ordinal));
    }

    [Fact]
    public void SizeStaysOnOneLineOrDropsTheUnit()
    {
        Assert.Equal(["1,310,720 bytes"], TableWrap.SizeLines("1,310,720 bytes", 20, Length));
        Assert.Equal(["1,310,720", "bytes"], TableWrap.SizeLines("1,310,720 bytes", 12, Length));
        Assert.Equal(["1,310,720", "bytes"], TableWrap.SizeLines("1,310,720 bytes", 8, Length));
        Assert.DoesNotContain(
            TableWrap.SizeLines("1,310,720 bytes", 8, Length),
            line => line is "1,310,72" or "0 bytes");
    }

    [Fact]
    public void HtmlPathBreaksAfterSeparatorsWithoutSplittingEntities()
    {
        Assert.Equal(
            "Documents/<wbr>messages.<wbr>sqlite",
            TableWrap.PathHtml("Documents/messages.sqlite"));
        Assert.Equal(
            "Exports/<wbr>extraction-<wbr>summary.<wbr>pdf",
            TableWrap.PathHtml("Exports/extraction-summary.pdf"));
        Assert.Equal("caf&#233;.<wbr>txt", TableWrap.PathHtml("caf\u00e9.txt"));
        Assert.Equal("Folder\\<wbr>file_<wbr>name.<wbr>txt", TableWrap.PathHtml("Folder\\file_name.txt"));
    }

    [Fact]
    public void HtmlRecordKeepsSizeOnOneLineAndBreaksALongPathSegment()
    {
        var record = SampleRecord();
        var html = HtmlReport.Render(record);
        Assert.Contains("td.size { text-align: right; white-space: nowrap; width: 1%; }", html, StringComparison.Ordinal);
        Assert.Contains("td.path { overflow-wrap: break-word; }", html, StringComparison.Ordinal);
        Assert.Contains("td.mono { overflow-wrap: anywhere; }", html, StringComparison.Ordinal);
        Assert.DoesNotContain("td { vertical-align: top; border-bottom: 1px solid #e4e0d8; padding: 8px 8px 8px 0; overflow-wrap: anywhere; }", html, StringComparison.Ordinal);
        Assert.Contains("Documents/<wbr>messages.<wbr>sqlite", html, StringComparison.Ordinal);
        Assert.Contains("DCIM/<wbr>Camera/<wbr>IMG_<wbr>0412.<wbr>jpg", html, StringComparison.Ordinal);
        Assert.Contains("1,310,720 bytes", html, StringComparison.Ordinal);
        Assert.Contains("quarterly-<wbr>extraction-<wbr>summary-<wbr>attachment.<wbr>sqlite", html, StringComparison.Ordinal);
        Assert.Contains("class=\"size\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("draft", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\u2014", html, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfRecordDoesNotSplitPathTokensOrTheSize()
    {
        var pdf = PdfReport.Render(SampleRecord());
        var text = Encoding.Latin1.GetString(pdf);
        var strings = PdfLiteralStrings(text);

        Assert.Contains("1,310,720 bytes", strings);
        Assert.DoesNotContain(strings, line => line is "1,310,720" or "1,310,72" or "0 bytes");
        Assert.Contains("Documents/messages.sqlite", strings);
        Assert.Contains("DCIM/Camera/IMG_0412.jpg", strings);
        Assert.Contains("Exports/extraction-summary.pdf", strings);
        Assert.Contains(strings, line => line.EndsWith('/'));
        Assert.DoesNotContain(strings, line =>
            (line.Contains("sqlit", StringComparison.Ordinal) && !line.Contains("sqlite", StringComparison.Ordinal))
            || (line.Contains("summar", StringComparison.Ordinal) && !line.Contains("summary", StringComparison.Ordinal))
            || line.EndsWith("IMG_0412.jp", StringComparison.Ordinal)
            || line.EndsWith("extraction-summar", StringComparison.Ordinal));
        Assert.DoesNotContain("draft", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\u2014", text, StringComparison.Ordinal);
    }

    private static double Length(string text) => text.Length;

    private static VerificationRecord SampleRecord()
    {
        var record = new VerificationRecord
        {
            VerifiedAt = new DateTimeOffset(2026, 10, 4, 15, 0, 0, TimeSpan.Zero),
            TimeZone = "UTC",
            MachineName = "EXAMINER-PC",
            Examiner = "A. Examiner",
            CaseReference = "SYNTH-001",
            Algorithm = "SHA-256",
            EvidencePath = @"D:\Evidence\SYNTH-001",
            EvidenceKind = "folder",
            SetHash = Sha256,
            ExpectedSource = "pasted hash",
            ExpectedFormat = "pasted hash",
            MatchCount = 4,
            FileCount = 4,
            Verdict = ResultLabels.AllMatchVerdict,
            Files =
            [
                File("Documents/messages.sqlite", 1_310_720),
                File("DCIM/Camera/IMG_0412.jpg", 2_457_600),
                File("Exports/extraction-summary.pdf", 88_064),
                File("Notes/2024/October/quarterly-extraction-summary-attachment.sqlite", 4_194_304),
            ],
        };
        RecordIntegrity.Stamp(record);
        return record;
    }

    private static FileVerificationEntry File(string path, long size)
    {
        return new FileVerificationEntry
        {
            Path = path,
            Size = size,
            ComputedHash = Sha256,
            ExpectedHash = Sha256,
            Result = ResultLabels.Match,
        };
    }

    private static List<string> PdfLiteralStrings(string text)
    {
        var found = new List<string>();
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '(')
            {
                continue;
            }

            var line = new StringBuilder();
            index++;
            while (index < text.Length)
            {
                var character = text[index++];
                if (character == '\\' && index < text.Length)
                {
                    var escaped = text[index++];
                    line.Append(escaped switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => escaped,
                    });
                    continue;
                }

                if (character == ')')
                {
                    break;
                }

                line.Append(character);
            }

            index--;
            found.Add(line.ToString());
        }

        return found;
    }
}
