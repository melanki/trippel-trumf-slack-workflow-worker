using melanki.trippeltrumf.service.Features.Polling;
using Xunit;

namespace melanki.trippeltrumf.service.tests.Features.Polling;

public sealed class DateExtractorTests
{
    [Fact]
    public void ExtractNext_ReturnsDate_WhenTextContainsDotSeparatedDayAndMonth()
    {
        var articleText = "Neste Trippel-Trumf er torsdag 26. mars.";
        var todayUtc = new DateOnly(2026, 3, 20);

        var result = DateExtractor.ExtractNext(articleText, todayUtc, referenceYear: 2026, referenceMonth: 3);

        Assert.Equal("2026-03-26", result.NextTrippelTrumfDate);
    }

    [Fact]
    public void ExtractNext_ReturnsDate_WhenTextContainsSpaceSeparatedDayAndMonth()
    {
        var articleText = "Neste Trippel-Trumf er torsdag 26 mars.";
        var todayUtc = new DateOnly(2026, 3, 20);

        var result = DateExtractor.ExtractNext(articleText, todayUtc, referenceYear: 2026, referenceMonth: 3);

        Assert.Equal("2026-03-26", result.NextTrippelTrumfDate);
    }

    [Fact]
    public void ExtractNext_RollsYear_WhenReferenceMonthIsDecemberAndExtractedMonthIsJanuary()
    {
        var articleText = "Neste Trippel-Trumf er torsdag 8. januar.";
        var todayUtc = new DateOnly(2025, 12, 10);

        var result = DateExtractor.ExtractNext(articleText, todayUtc, referenceYear: 2025, referenceMonth: 12);

        Assert.Equal("2026-01-08", result.NextTrippelTrumfDate);
    }
}
