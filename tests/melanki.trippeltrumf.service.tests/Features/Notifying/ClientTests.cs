using System.Net;
using System.Text.Json;
using melanki.trippeltrumf.service.Features.Polling;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace melanki.trippeltrumf.service.tests.Features.Notifying;

public sealed class ClientTests
{
    [Fact]
    public async Task NotifyStateChangeAsync_SkipsWebhookCall_WhenDateIsMissing()
    {
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new melanki.trippeltrumf.service.Features.Notifying.Options
        {
            SlackWorkflowWebhookUrl = "https://example.invalid/webhook"
        });
        var client = new melanki.trippeltrumf.service.Features.Notifying.Client(
            httpClient,
            options,
            NullLogger<melanki.trippeltrumf.service.Features.Notifying.Client>.Instance);

        var change = new StateChange(
            Reason: "startup",
            Snapshot: new StateSnapshot(
                Result: new Result(null),
                ParsedNextDate: null,
                CachedAtUtc: null,
                LastAttemptAtUtc: DateTimeOffset.UtcNow,
                LastError: "scrape failed",
                ReferenceYear: null,
                ReferenceMonth: null));

        var sent = await client.NotifyStateChangeAsync(change, CancellationToken.None);

        Assert.False(sent);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task NotifyStateChangeAsync_PostsWebhookPayload_WhenDateExists()
    {
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new melanki.trippeltrumf.service.Features.Notifying.Options
        {
            SlackWorkflowWebhookUrl = "https://example.invalid/webhook"
        });
        var client = new melanki.trippeltrumf.service.Features.Notifying.Client(
            httpClient,
            options,
            NullLogger<melanki.trippeltrumf.service.Features.Notifying.Client>.Instance);

        var change = new StateChange(
            Reason: "startup",
            Snapshot: new StateSnapshot(
                Result: new Result("2026-03-26"),
                ParsedNextDate: new DateOnly(2026, 3, 26),
                CachedAtUtc: DateTimeOffset.UtcNow,
                LastAttemptAtUtc: DateTimeOffset.UtcNow,
                LastError: null,
                ReferenceYear: 2026,
                ReferenceMonth: 3));

        var sent = await client.NotifyStateChangeAsync(change, CancellationToken.None);

        Assert.True(sent);
        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(handler.RequestBody);

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(
            "2026-03-26",
            payload.RootElement.GetProperty("trippel_trumf_date").GetString());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
