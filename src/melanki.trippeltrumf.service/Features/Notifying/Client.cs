using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
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

    public async Task<bool> NotifyStateChangeAsync(StateChange change, CancellationToken cancellationToken)
    {
        var webhookUrl = _options.Value.SlackWorkflowWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogDebug("Slack notification skipped: TrippelTrumfService:SlackWorkflowWebhookUrl is not configured.");
            return false;
        }

        var nextDate = BuildDate(change);
        if (string.IsNullOrWhiteSpace(nextDate))
        {
            _logger.LogWarning(
                "Slack notification skipped ({Reason}): next Trippel-Trumf date is missing. LastError {LastError}",
                change.Reason,
                change.Snapshot.LastError);
            return false;
        }

        var payload = new
        {
            trippel_trumf_date = nextDate
        };
        var endpoint = DescribeWebhookEndpoint(webhookUrl);
        var requestDuration = Stopwatch.StartNew();
        _logger.LogDebug(
            "Sending Slack webhook POST. Reason {Reason}, NextTrippelTrumfDate {NextDate}, WebhookHost {WebhookHost}, WebhookPathHash {WebhookPathHash}",
            change.Reason,
            payload.trippel_trumf_date,
            endpoint.Host,
            endpoint.PathHash);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            requestDuration.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var responsePreview = Truncate(body, 512);
                _logger.LogWarning(
                    "Slack webhook POST failed. Reason {Reason}, StatusCode {StatusCode}, DurationMs {DurationMs}, WebhookHost {WebhookHost}, WebhookPathHash {WebhookPathHash}, ResponseBodyPreview {ResponseBodyPreview}",
                    change.Reason,
                    (int)response.StatusCode,
                    requestDuration.ElapsedMilliseconds,
                    endpoint.Host,
                    endpoint.PathHash,
                    responsePreview);

                throw new InvalidOperationException(
                    $"Slack webhook returned {(int)response.StatusCode}: {responsePreview}");
            }

            _logger.LogInformation(
                "Slack webhook POST succeeded. Reason {Reason}, StatusCode {StatusCode}, DurationMs {DurationMs}, WebhookHost {WebhookHost}, WebhookPathHash {WebhookPathHash}",
                change.Reason,
                (int)response.StatusCode,
                requestDuration.ElapsedMilliseconds,
                endpoint.Host,
                endpoint.PathHash);
            return true;
        }
        catch (HttpRequestException exception)
        {
            requestDuration.Stop();
            _logger.LogWarning(
                exception,
                "Slack webhook POST request error. Reason {Reason}, DurationMs {DurationMs}, WebhookHost {WebhookHost}, WebhookPathHash {WebhookPathHash}",
                change.Reason,
                requestDuration.ElapsedMilliseconds,
                endpoint.Host,
                endpoint.PathHash);
            throw;
        }
    }

    private static string BuildDate(StateChange change)
    {
        var snapshot = change.Snapshot;
        return snapshot.Result?.NextTrippelTrumfDate ?? string.Empty;
    }

    private static (string Host, string PathHash) DescribeWebhookEndpoint(string webhookUrl)
    {
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri))
        {
            return ("invalid", "invalid");
        }

        var path = uri.AbsolutePath;
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        var hash = Convert.ToHexString(hashBytes.AsSpan(0, 8));

        return (uri.Host, hash);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
