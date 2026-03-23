using melanki.trippeltrumf.service.Features.Notifying;
using Xunit;

namespace melanki.trippeltrumf.service.tests.Features.Notifying;

public sealed class ReminderPolicyTests
{
    [Fact]
    public void Evaluate_ReturnsEligible_OnCalendarDayBefore_WhenNotAlreadySent()
    {
        var nextDate = new DateOnly(2026, 12, 10);
        var today = new DateOnly(2026, 12, 9);

        var result = Reminder.Evaluate(nextDate, today, alreadySent: false);

        Assert.Equal(ReminderEligibility.Eligible, result);
    }

    [Fact]
    public void Evaluate_ReturnsNoUpcomingDate_WhenDateIsMissing()
    {
        var today = new DateOnly(2026, 12, 9);

        var result = Reminder.Evaluate(nextTrippelTrumfDate: null, today, alreadySent: false);

        Assert.Equal(ReminderEligibility.NoUpcomingDate, result);
    }

    [Fact]
    public void Evaluate_ReturnsNotDayBefore_WhenTodayDoesNotMatchReminderDate()
    {
        var nextDate = new DateOnly(2026, 12, 10);
        var today = new DateOnly(2026, 12, 8);

        var result = Reminder.Evaluate(nextDate, today, alreadySent: false);

        Assert.Equal(ReminderEligibility.NotDayBefore, result);
    }

    [Fact]
    public void Evaluate_ReturnsAlreadySent_WhenReminderAlreadySentForEventDate()
    {
        var nextDate = new DateOnly(2026, 12, 10);
        var today = new DateOnly(2026, 12, 9);

        var result = Reminder.Evaluate(nextDate, today, alreadySent: true);

        Assert.Equal(ReminderEligibility.AlreadySent, result);
    }
}
