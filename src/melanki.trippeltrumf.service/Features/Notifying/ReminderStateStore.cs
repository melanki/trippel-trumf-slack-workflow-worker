namespace melanki.trippeltrumf.service.Features.Notifying;

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
