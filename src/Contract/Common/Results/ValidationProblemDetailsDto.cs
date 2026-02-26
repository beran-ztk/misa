using System.Text.Json.Serialization;

namespace Misa.Contract.Common.Results;

public sealed class ValidationProblemDetailsDto : ProblemDetailsDto
{
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; init; }
}