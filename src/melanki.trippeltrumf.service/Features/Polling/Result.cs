using System.Text.Json.Serialization;

namespace melanki.trippeltrumf.service.Features.Polling;

public sealed record Result(
    [property: JsonPropertyName("nextTrippelTrumfDate")] string? NextTrippelTrumfDate);
