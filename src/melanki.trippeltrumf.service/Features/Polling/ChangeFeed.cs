using System.Threading.Channels;

namespace melanki.trippeltrumf.service.Features.Polling;

public sealed class ChangeFeed
{
    private readonly Channel<StateChange> _channel = Channel.CreateUnbounded<StateChange>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public void Publish(StateChange change)
    {
        _channel.Writer.TryWrite(change);
    }

    public IAsyncEnumerable<StateChange> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

public sealed record StateChange(
    string Reason,
    StateSnapshot Snapshot);
