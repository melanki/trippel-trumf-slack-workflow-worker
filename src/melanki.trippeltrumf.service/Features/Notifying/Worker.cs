using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using melanki.trippeltrumf.service.Features.Polling;

namespace melanki.trippeltrumf.service.Features.Notifying;

public sealed class Worker : BackgroundService
{
    private readonly ChangeFeed _changeFeed;
    private readonly Client _client;
    private readonly ILogger<Worker> _logger;

    public Worker(
        ChangeFeed changeFeed,
        Client client,
        ILogger<Worker> logger)
    {
        _changeFeed = changeFeed;
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var change in _changeFeed.ReadAllAsync(stoppingToken))
        {
            _logger.LogDebug(
                "Received state change event. Reason {Reason}, NextTrippelTrumfDate {NextDate}, Error {Error}",
                change.Reason,
                change.Snapshot.Result?.NextTrippelTrumfDate,
                change.Snapshot.LastError);

            try
            {
                await _client.NotifyStateChangeAsync(change, stoppingToken);
                _logger.LogInformation("Published state change to Slack ({Reason}).", change.Reason);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to publish state change to Slack ({Reason}).", change.Reason);
            }
        }
    }
}
