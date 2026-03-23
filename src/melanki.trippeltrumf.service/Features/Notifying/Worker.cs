using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using melanki.trippeltrumf.service.Features.Polling;

namespace melanki.trippeltrumf.service.Features.Notifying;

public sealed class Worker : BackgroundService
{
    private const string OsloTimeZoneId = "Europe/Oslo";
    private static readonly TimeSpan ReminderCheckInterval = TimeSpan.FromHours(12);

    private readonly ChangeFeed _changeFeed;
    private readonly StateStore _stateStore;
    private readonly ReminderStateStore _reminderStateStore;
    private readonly Client _client;
    private readonly ILogger<Worker> _logger;
    private readonly TimeZoneInfo _reminderTimeZone;

    public Worker(
        ChangeFeed changeFeed,
        StateStore stateStore,
        ReminderStateStore reminderStateStore,
        Client client,
        ILogger<Worker> logger)
    {
        _changeFeed = changeFeed;
        _stateStore = stateStore;
        _reminderStateStore = reminderStateStore;
        _client = client;
        _logger = logger;
        _reminderTimeZone = ResolveTimeZone(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Notifying worker started. TimeZoneId {TimeZoneId}, ReminderCheckInterval {ReminderCheckInterval}.",
            _reminderTimeZone.Id,
            ReminderCheckInterval);

        var changeFeedTask = ProcessStateChangesAsync(stoppingToken);
        var reminderTask = RunReminderLoopAsync(stoppingToken);
        await Task.WhenAll(changeFeedTask, reminderTask);
    }

    private async Task ProcessStateChangesAsync(CancellationToken stoppingToken)
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

    private async Task RunReminderLoopAsync(CancellationToken stoppingToken)
    {
        await CheckReminderAsync("startup", stoppingToken);

        using var timer = new PeriodicTimer(ReminderCheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckReminderAsync("scheduled", stoppingToken);
        }
    }

    private async Task CheckReminderAsync(string reason, CancellationToken cancellationToken)
    {
        var snapshot = _stateStore.GetSnapshot();
        var nextDate = snapshot.ParsedNextDate;
        var todayInReminderTimeZone = GetTodayInReminderTimeZone();
        if (nextDate is null)
        {
            _logger.LogDebug(
                "Reminder check skipped ({Reason}). No cached next date. TodayInReminderTimeZone {Today}, TimeZoneId {TimeZoneId}",
                reason,
                todayInReminderTimeZone,
                _reminderTimeZone.Id);
            return;
        }

        var alreadySent = _reminderStateStore.HasSent(nextDate.Value);
        var eligibility = Reminder.Evaluate(nextDate, todayInReminderTimeZone, alreadySent);
        if (eligibility != ReminderEligibility.Eligible)
        {
            _logger.LogDebug(
                "Reminder check not eligible ({Reason}). NextDate {NextDate}, TodayInReminderTimeZone {Today}, Eligibility {Eligibility}, TimeZoneId {TimeZoneId}",
                reason,
                nextDate,
                todayInReminderTimeZone,
                eligibility,
                _reminderTimeZone.Id);
            return;
        }

        try
        {
            await _client.NotifyStateChangeAsync(
                new StateChange("day-before-reminder", snapshot),
                cancellationToken);
            _reminderStateStore.TryMarkSent(nextDate.Value);
            _logger.LogInformation(
                "Published day-before reminder for date {NextDate}. TimeZoneId {TimeZoneId}",
                nextDate,
                _reminderTimeZone.Id);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to publish day-before reminder for date {NextDate}.",
                nextDate);
        }
    }

    private DateOnly GetTodayInReminderTimeZone()
    {
        var nowInReminderTimeZone = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _reminderTimeZone);
        return DateOnly.FromDateTime(nowInReminderTimeZone.DateTime);
    }

    private static TimeZoneInfo ResolveTimeZone(ILogger logger)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(OsloTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            logger.LogWarning("Oslo timezone id {TimeZoneId} was not found. Falling back to UTC.", OsloTimeZoneId);
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            logger.LogWarning("Oslo timezone id {TimeZoneId} is invalid. Falling back to UTC.", OsloTimeZoneId);
            return TimeZoneInfo.Utc;
        }
    }
}
