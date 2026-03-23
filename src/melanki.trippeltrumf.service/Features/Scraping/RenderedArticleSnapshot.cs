namespace melanki.trippeltrumf.service.Features.Scraping;

public sealed record RenderedArticleSnapshot(
    string ArticleText,
    int? DateModifiedYear,
    int? DateModifiedMonth);
