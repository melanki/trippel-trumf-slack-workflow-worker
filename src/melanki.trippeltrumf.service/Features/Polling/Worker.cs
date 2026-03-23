using melanki.trippeltrumf.service.Features.Scraping;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace melanki.trippeltrumf.service.Features.Polling;

public sealed class Worker : BackgroundService
{
    private readonly Scraper _scraper;
    private readonly StateStore _stateStore;
    private readonly ChangeFeed _changeFeed;
    private readonly ILogger<Worker> _logger;

    public Worker(
        Scraper scraper,
        StateStore stateStore,
        ChangeFeed changeFeed,
        ILogger<Worker> logger)
    {
        _scraper = scraper;
        _stateStore = stateStore;
        _changeFeed = changeFeed;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshIfNeededAsync("startup", stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextUtcMidnight();
            _logger.LogDebug("Next refresh check scheduled in {Delay}.", delay);

            await Task.Delay(delay, stoppingToken);
            await RefreshIfNeededAsync("midnight", stoppingToken);
        }
    }

    private async Task RefreshIfNeededAsync(string reason, CancellationToken cancellationToken)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentSnapshot = _stateStore.GetSnapshot();
        _logger.LogDebug(
            "Refresh check {Reason}. TodayUtc {TodayUtc}, CachedNextDate {CachedNextDate}, LastError {LastError}",
            reason,
            todayUtc,
            currentSnapshot.ParsedNextDate,
            currentSnapshot.LastError);

        if (!_stateStore.RequiresRefresh(todayUtc))
        {
            _logger.LogDebug("Refresh skipped ({Reason}): cached date is still in the future.", reason);
            return;
        }

        _stateStore.MarkAttempt();
        _logger.LogInformation("Refreshing next Trippel-Trumf date ({Reason}).", reason);

        try
        {
            var before = currentSnapshot;
            var snapshot = await _scraper.GetRenderedArticleSnapshot(cancellationToken);
            _logger.LogDebug(
                "Scrape snapshot {Reason}. ArticleTextLength {ArticleTextLength}, DateModifiedYear {DateModifiedYear}, DateModifiedMonth {DateModifiedMonth}",
                reason,
                snapshot.ArticleText.Length,
                snapshot.DateModifiedYear,
                snapshot.DateModifiedMonth);

            var result = DateExtractor.ExtractNext(
                snapshot.ArticleText,
                todayUtc,
                snapshot.DateModifiedYear,
                snapshot.DateModifiedMonth);
            _logger.LogDebug("Extracted next date {Reason}. NextTrippelTrumfDate {NextDate}", reason, result.NextTrippelTrumfDate);

            _stateStore.UpdateSuccess(result, snapshot.DateModifiedYear, snapshot.DateModifiedMonth);
            var after = _stateStore.GetSnapshot();
            PublishStateChangeIfNeeded(before, after, reason);
            _logger.LogInformation("Refresh completed. NextTrippelTrumfDate {NextDate}", result.NextTrippelTrumfDate);
        }
        catch (Exception exception)
        {
            var before = _stateStore.GetSnapshot();
            _stateStore.UpdateFailure(exception.Message);
            var after = _stateStore.GetSnapshot();
            PublishStateChangeIfNeeded(before, after, reason);
            _logger.LogWarning(exception, "Refresh failed ({Reason}).", reason);
        }
    }

    private static TimeSpan GetDelayUntilNextUtcMidnight()
    {
        var now = DateTimeOffset.UtcNow;
        var nextMidnightUtc = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
        var delay = nextMidnightUtc - now;

        return delay > TimeSpan.Zero ? delay : TimeSpan.FromMinutes(1);
    }

    private void PublishStateChangeIfNeeded(
        StateSnapshot before,
        StateSnapshot after,
        string reason)
    {
        var hasChanged =
            before.Result?.NextTrippelTrumfDate != after.Result?.NextTrippelTrumfDate ||
            before.LastError != after.LastError ||
            before.ReferenceYear != after.ReferenceYear ||
            before.ReferenceMonth != after.ReferenceMonth;

        if (!hasChanged)
        {
            _logger.LogDebug("State unchanged ({Reason}); no change event published.", reason);
            return;
        }

        _logger.LogDebug(
            "State changed {Reason}. BeforeDate {BeforeDate}, AfterDate {AfterDate}, BeforeError {BeforeError}, AfterError {AfterError}",
            reason,
            before.Result?.NextTrippelTrumfDate,
            after.Result?.NextTrippelTrumfDate,
            before.LastError,
            after.LastError);
        _changeFeed.Publish(new StateChange(reason, after));
    }
}
