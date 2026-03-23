namespace melanki.trippeltrumf.service.Features.Notifying;

public sealed class Options
{
    public const string SectionName = "TrippelTrumfService";

    public string? SlackWorkflowWebhookUrl { get; init; }
}
