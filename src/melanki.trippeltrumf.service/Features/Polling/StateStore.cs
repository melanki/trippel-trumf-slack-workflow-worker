using System.Globalization;
using Microsoft.Extensions.Logging;

namespace melanki.trippeltrumf.service.Features.Polling;

public sealed class StateStore
{
    private readonly ILogger<StateStore> _logger;
    private readonly object _lock = new();
    private Result? _result;
    private DateOnly? _nextDate;
    private DateTimeOffset? _cachedAtUtc;
    private DateTimeOffset? _lastAttemptAtUtc;
    private string? _lastError;
    private int? _referenceYear;
    private int? _referenceMonth;

    public StateStore(ILogger<StateStore> logger)
    {
        _logger = logger;
    }

    public bool RequiresRefresh(DateOnly todayUtc)
    {
        lock (_lock)
        {
            var requiresRefresh = _nextDate is null || _nextDate <= todayUtc;
            _logger.LogDebug(
                "RequiresRefresh evaluated. todayUtc={TodayUtc}, cachedNextDate={CachedNextDate}, requiresRefresh={RequiresRefresh}",
                todayUtc,
                _nextDate,
                requiresRefresh);
            return requiresRefresh;
        }
    }

    public void MarkAttempt()
    {
        lock (_lock)
        {
            _lastAttemptAtUtc = DateTimeOffset.UtcNow;
            _lastError = null;
            _logger.LogDebug("Marked refresh attempt at {LastAttemptAtUtc}.", _lastAttemptAtUtc);
        }
    }

    public void UpdateSuccess(Result result, int? referenceYear, int? referenceMonth)
    {
        lock (_lock)
        {
            _result = result;
            _cachedAtUtc = DateTimeOffset.UtcNow;
            _referenceYear = referenceYear;
            _referenceMonth = referenceMonth;
            _lastError = null;

            if (DateOnly.TryParseExact(
                result.NextTrippelTrumfDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
            {
                _nextDate = parsedDate;
            }
            else
            {
                _nextDate = null;
            }

            _logger.LogDebug(
                "State success update. nextTrippelTrumfDate={NextDate}, parsedNextDate={ParsedNextDate}, referenceYear={ReferenceYear}, referenceMonth={ReferenceMonth}, cachedAtUtc={CachedAtUtc}",
                result.NextTrippelTrumfDate,
                _nextDate,
                _referenceYear,
                _referenceMonth,
                _cachedAtUtc);
        }
    }

    public void UpdateFailure(string error)
    {
        lock (_lock)
        {
            _lastError = error;
            _logger.LogDebug("State failure update. error={Error}", _lastError);
        }
    }

    public StateSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new StateSnapshot(
                Result: _result,
                ParsedNextDate: _nextDate,
                CachedAtUtc: _cachedAtUtc,
                LastAttemptAtUtc: _lastAttemptAtUtc,
                LastError: _lastError,
                ReferenceYear: _referenceYear,
                ReferenceMonth: _referenceMonth);
        }
    }
}

public sealed record StateSnapshot(
    Result? Result,
    DateOnly? ParsedNextDate,
    DateTimeOffset? CachedAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    string? LastError,
    int? ReferenceYear,
    int? ReferenceMonth);
