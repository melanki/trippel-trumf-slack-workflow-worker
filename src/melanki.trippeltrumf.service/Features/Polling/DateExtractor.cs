using System.Text.RegularExpressions;

namespace melanki.trippeltrumf.service.Features.Polling;

public static class DateExtractor
{
    private static readonly Regex DatePattern = new(
        @"\b(?<day>[1-9]|[12]\d|3[01])\.\s*(?<month>januar|februar|mars|april|mai|juni|juli|august|september|oktober|november|desember)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Dictionary<string, int> NorwegianMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["januar"] = 1,
        ["februar"] = 2,
        ["mars"] = 3,
        ["april"] = 4,
        ["mai"] = 5,
        ["juni"] = 6,
        ["juli"] = 7,
        ["august"] = 8,
        ["september"] = 9,
        ["oktober"] = 10,
        ["november"] = 11,
        ["desember"] = 12
    };

    public static Result ExtractNext(string articleText, DateOnly todayUtc, int? referenceYear, int? referenceMonth)
    {
        if (string.IsNullOrWhiteSpace(articleText))
        {
            return new Result(NextTrippelTrumfDate: null);
        }

        DateOnly? nextDate = null;
        var matches = DatePattern.Matches(articleText);
        foreach (Match match in matches)
        {
            if (!match.Success)
            {
                continue;
            }

            if (!int.TryParse(match.Groups["day"].Value, out var day))
            {
                continue;
            }

            var monthName = match.Groups["month"].Value;
            if (!NorwegianMonths.TryGetValue(monthName, out var month))
            {
                continue;
            }

            if (!TryCreateCandidateDate(todayUtc, referenceYear, referenceMonth, day, month, out var candidate))
            {
                continue;
            }

            if (nextDate is null || candidate < nextDate.Value)
            {
                nextDate = candidate;
            }
        }

        return new Result(
            NextTrippelTrumfDate: nextDate?.ToString("yyyy-MM-dd"));
    }

    private static bool TryCreateCandidateDate(DateOnly todayUtc, int? referenceYear, int? referenceMonth, int day, int month, out DateOnly candidate)
    {
        candidate = default;

        if (referenceYear is int explicitYear)
        {
            var referenceCandidateYear = referenceMonth == 12 && month == 1
                ? explicitYear + 1
                : explicitYear;

            return TryCreateDate(referenceCandidateYear, month, day, out candidate);
        }

        return TryCreateDate(todayUtc.Year, month, day, out candidate);
    }

    private static bool TryCreateDate(int year, int month, int day, out DateOnly date)
    {
        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            date = default;
            return false;
        }
    }
}
