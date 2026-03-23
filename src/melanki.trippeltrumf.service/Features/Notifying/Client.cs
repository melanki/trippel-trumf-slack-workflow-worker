using System.Net.Http.Json;
using melanki.trippeltrumf.service.Features.Polling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace melanki.trippeltrumf.service.Features.Notifying;

public sealed class Client
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<Options> _options;
    private readonly ILogger<Client> _logger;

    public Client(
        HttpClient httpClient,
        IOptions<Options> options,
        ILogger<Client> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task NotifyStateChangeAsync(StateChange change, CancellationToken cancellationToken)
    {
        var webhookUrl = _options.Value.SlackWorkflowWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogDebug("Slack notification skipped: TrippelTrumfService:SlackWorkflowWebhookUrl is not configured.");
            return;
        }

        var payload = new
        {
            trippel_trumf_date = BuildDate(change)
        };
        _logger.LogDebug(
            "Posting Slack notification. Reason {Reason}, NextTrippelTrumfDate {NextDate}",
            change.Reason,
            payload.trippel_trumf_date);

        using var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Slack webhook returned {(int)response.StatusCode}: {body}");
        }

        _logger.LogDebug(
            "Slack notification succeeded. StatusCode {StatusCode}, Reason {Reason}",
            (int)response.StatusCode,
            change.Reason);
    }

    private static string BuildDate(StateChange change)
    {
        var snapshot = change.Snapshot;
        return snapshot.Result?.NextTrippelTrumfDate ?? string.Empty;
    }
}
