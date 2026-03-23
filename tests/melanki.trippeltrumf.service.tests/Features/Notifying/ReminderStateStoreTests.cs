using melanki.trippeltrumf.service.Features.Notifying;
using Xunit;

namespace melanki.trippeltrumf.service.tests.Features.Notifying;

public sealed class ReminderStateStoreTests
{
    [Fact]
    public void TryMarkSent_IsIdempotent_ForSameEventDate()
    {
        var store = new ReminderStateStore();
        var eventDate = new DateOnly(2026, 3, 14);

        var first = store.TryMarkSent(eventDate);
        var second = store.TryMarkSent(eventDate);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void HasSent_ReturnsTrue_AfterMarkingEventDate()
    {
        var store = new ReminderStateStore();
        var eventDate = new DateOnly(2026, 3, 14);

        store.TryMarkSent(eventDate);

        Assert.True(store.HasSent(eventDate));
    }
}
