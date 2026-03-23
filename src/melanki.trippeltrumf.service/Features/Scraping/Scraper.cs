using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace melanki.trippeltrumf.service.Features.Scraping;

public class Scraper
{
    private const string TrippelTrumfPageUrl = "https://www.trumf.no/trippel-trumf";
    private readonly ILogger<Scraper> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Scraper(ILogger<Scraper> logger)
    {
        _logger = logger;
    }

    public Task<string> GetPage(CancellationToken cancellationToken = default)
    {
        return WithPage(
            page => page.ContentAsync(),
            cancellationToken);
    }

    public async Task<StructuredTrippelTrumfPage> GetStructuredPage(CancellationToken cancellationToken = default)
    {
        var snapshotJson = await WithPage(
            page => page.EvaluateAsync<string>(
                @"() => {
                    const title = document.title || '';
                    const article = document.querySelector('article');
                    const canonicalUrl = document.querySelector('link[rel=""canonical""]')?.href ?? null;
                    const description = document.querySelector('meta[name=""description""]')?.content ?? null;
                    const articleHtml = article ? article.innerHTML : '';
                    const paragraphNodes = article ? article.querySelectorAll('p') : [];

                    const paragraphs = Array.from(paragraphNodes)
                        .map((node) => (node.textContent || '').replace(/\s+/g, ' ').trim())
                        .filter((value) => value.length > 0);

                    return JSON.stringify({
                        url: window.location.href,
                        title,
                        canonicalUrl,
                        description,
                        articleHtml,
                        paragraphs
                    });
                }"),
            cancellationToken);

        var snapshot = JsonSerializer.Deserialize<PageSnapshot>(snapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize structured page snapshot.");
        _logger.LogDebug(
            "Structured page scraped. Url {Url}, Title {Title}, ParagraphCount {ParagraphCount}, ArticleHtmlLength {ArticleHtmlLength}",
            snapshot.Url,
            snapshot.Title,
            snapshot.Paragraphs.Count,
            snapshot.ArticleHtml.Length);

        return new StructuredTrippelTrumfPage(
            Url: snapshot.Url,
            Title: snapshot.Title,
            CanonicalUrl: snapshot.CanonicalUrl,
            Description: snapshot.Description,
            ArticleHtml: snapshot.ArticleHtml,
            Paragraphs: snapshot.Paragraphs,
            RetrievedAtUtc: DateTimeOffset.UtcNow);
    }

    public Task<string> GetRenderedArticleText(CancellationToken cancellationToken = default)
    {
        return WithPage(
            page => page.EvaluateAsync<string>(
                @"() => {
                    const article = document.querySelector('article');
                    if (!article) {
                        return '';
                    }

                    return (article.innerText || article.textContent || '')
                        .replace(/\s+/g, ' ')
                        .trim();
                }"),
            cancellationToken);
    }

    public async Task<RenderedArticleSnapshot> GetRenderedArticleSnapshot(CancellationToken cancellationToken = default)
    {
        var snapshotJson = await WithPage(
            page => page.EvaluateAsync<string>(
                @"() => {
                    const article = document.querySelector('article');
                    const articleText = article
                        ? (article.innerText || article.textContent || '').replace(/\s+/g, ' ').trim()
                        : '';

                    const findDateModified = (value) => {
                        if (!value || typeof value !== 'object') {
                            return null;
                        }

                        if (Array.isArray(value)) {
                            for (const item of value) {
                                const found = findDateModified(item);
                                if (found) {
                                    return found;
                                }
                            }
                            return null;
                        }

                        if (typeof value.dateModified === 'string' && value.dateModified.trim().length > 0) {
                            return value.dateModified.trim();
                        }

                        for (const key of Object.keys(value)) {
                            const found = findDateModified(value[key]);
                            if (found) {
                                return found;
                            }
                        }

                        return null;
                    };

                    let dateModified = null;
                    const scriptNodes = Array.from(document.querySelectorAll('script[type=""application/ld+json""]'));
                    for (const scriptNode of scriptNodes) {
                        const raw = scriptNode.textContent || '';
                        if (!raw) {
                            continue;
                        }

                        try {
                            const parsed = JSON.parse(raw);
                            const found = findDateModified(parsed);
                            if (found) {
                                dateModified = found;
                                break;
                            }
                        } catch {
                            // Ignore malformed JSON-LD scripts.
                        }
                    }

                    return JSON.stringify({
                        articleText,
                        dateModified
                    });
                }"),
            cancellationToken);

        var snapshot = JsonSerializer.Deserialize<RenderedArticleSnapshotDto>(snapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize rendered article snapshot.");

        var dateModifiedParts = ParseDateParts(snapshot.DateModified);
        _logger.LogDebug(
            "Rendered article scraped. ArticleTextLength {ArticleTextLength}, DateModifiedRaw {DateModifiedRaw}, DateModifiedYear {DateModifiedYear}, DateModifiedMonth {DateModifiedMonth}",
            snapshot.ArticleText.Length,
            snapshot.DateModified,
            dateModifiedParts.Year,
            dateModifiedParts.Month);

        return new RenderedArticleSnapshot(
            ArticleText: snapshot.ArticleText,
            DateModifiedYear: dateModifiedParts.Year,
            DateModifiedMonth: dateModifiedParts.Month);
    }

    private static async Task<T> WithPage<T>(Func<IPage, Task<T>> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await LaunchBrowser(playwright);
        var page = await browser.NewPageAsync();

        await page.GotoAsync(TrippelTrumfPageUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        cancellationToken.ThrowIfCancellationRequested();
        return await action(page);
    }

    private static async Task<IBrowser> LaunchBrowser(IPlaywright playwright)
    {
        try
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = "chrome"
            });
        }
        catch (PlaywrightException)
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }
    }

    private sealed class PageSnapshot
    {
        public string Url { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? CanonicalUrl { get; init; }
        public string? Description { get; init; }
        public string ArticleHtml { get; init; } = string.Empty;
        public IReadOnlyList<string> Paragraphs { get; init; } = [];
    }

    private sealed class RenderedArticleSnapshotDto
    {
        public string ArticleText { get; init; } = string.Empty;
        public string? DateModified { get; init; }
    }

    private static (int? Year, int? Month) ParseDateParts(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed))
        {
            return (parsed.Year, parsed.Month);
        }

        return (null, null);
    }
}
