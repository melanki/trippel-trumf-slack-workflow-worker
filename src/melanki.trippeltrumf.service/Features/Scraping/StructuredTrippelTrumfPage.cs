namespace melanki.trippeltrumf.service.Features.Scraping;

public sealed record StructuredTrippelTrumfPage(
    string Url,
    string Title,
    string? CanonicalUrl,
    string? Description,
    string ArticleHtml,
    IReadOnlyList<string> Paragraphs,
    DateTimeOffset RetrievedAtUtc);
