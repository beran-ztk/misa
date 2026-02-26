using System.Text.Json.Serialization;

namespace Misa.Contract.Common.Results;

public class ProblemDetailsDto
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("status")]
    public int? Status { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("instance")]
    public string? Instance { get; init; }

    // Extensions (z.B. traceId) landen bei System.Text.Json standardmäßig NICHT automatisch hier,
    // außer du modellierst sie explizit:
    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }
}