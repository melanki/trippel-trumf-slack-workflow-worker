namespace melanki.trippeltrumf.service.Features.Notifying;

public static class Reminder
{
    public static ReminderEligibility Evaluate(DateOnly? nextTrippelTrumfDate, DateOnly todayInReminderTimeZone, bool alreadySent)
    {
        if (nextTrippelTrumfDate is null)
        {
            return ReminderEligibility.NoUpcomingDate;
        }

        if (alreadySent)
        {
            return ReminderEligibility.AlreadySent;
        }

        var reminderDate = nextTrippelTrumfDate.Value.AddDays(-1);
        return reminderDate == todayInReminderTimeZone
            ? ReminderEligibility.Eligible
            : ReminderEligibility.NotDayBefore;
    }
}

public enum ReminderEligibility
{
    Eligible = 0,
    NoUpcomingDate = 1,
    NotDayBefore = 2,
    AlreadySent = 3
}

public sealed class ReminderStateStore
{
    private readonly object _lock = new();
    private readonly HashSet<DateOnly> _sentReminderDates = [];

    public bool HasSent(DateOnly eventDate)
    {
        lock (_lock)
        {
            return _sentReminderDates.Contains(eventDate);
        }
    }

    public bool TryMarkSent(DateOnly eventDate)
    {
        lock (_lock)
        {
            return _sentReminderDates.Add(eventDate);
        }
    }
}
