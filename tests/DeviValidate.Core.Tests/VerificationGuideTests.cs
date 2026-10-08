using DeviValidate.Core.Reporting;

namespace DeviValidate.Core.Tests;

public class VerificationGuideTests
{
    [Fact]
    public void GuideTellsTheUserWhereTheComparisonHashComesFrom()
    {
        var text = VerificationGuide.Banner + "\n" + VerificationGuide.RecordSummary + "\n"
            + string.Join("\n", VerificationGuide.Sections.Select(section => section.Heading + "\n" + section.Body));

        Assert.Contains("physical analyzer", text, StringComparison.Ordinal);
        Assert.Contains("You do not re-image the drive", text, StringComparison.Ordinal);
        Assert.Contains("does not connect to the analyzer", VerificationGuide.Banner, StringComparison.Ordinal);
        Assert.Contains("FTK Imager", text, StringComparison.Ordinal);
        Assert.Contains("E01", text, StringComparison.Ordinal);
        Assert.Contains("certutil -hashfile", text, StringComparison.Ordinal);
        Assert.Contains("Get-FileHash", text, StringComparison.Ordinal);
        Assert.Contains("sha256sum", text, StringComparison.Ordinal);
        Assert.Contains("Hash only", text, StringComparison.Ordinal);
        Assert.Contains("prosecutor", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("defense", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("32 hex characters", text, StringComparison.Ordinal);
        Assert.Contains("SHA-256 is 64", text, StringComparison.Ordinal);
        Assert.Contains(ReportCopy.Independent, text, StringComparison.Ordinal);
        Assert.Contains(ReportCopy.MatchMeans, text, StringComparison.Ordinal);
        Assert.Contains(ReportCopy.RedMeans, text, StringComparison.Ordinal);
        Assert.DoesNotContain("draft", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\u2014", text, StringComparison.Ordinal);
        Assert.Equal(14, VerificationGuide.Sections.Count);
        Assert.All(VerificationGuide.Sections, section =>
        {
            Assert.False(string.IsNullOrWhiteSpace(section.Heading));
            Assert.False(string.IsNullOrWhiteSpace(section.Body));
        });
    }

    [Fact]
    public void RecordPrintsTheVerificationProcedure()
    {
        var section = Assert.Single(ReportCopy.Explanation, item => item.Heading == "What you do to verify");
        Assert.Equal(VerificationGuide.RecordSummary, section.Body);
        Assert.Contains("physical analyzer", section.Body, StringComparison.Ordinal);
        Assert.Contains("defense attorney", section.Body, StringComparison.Ordinal);
    }
}
