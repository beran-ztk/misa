namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

public record SessionSegmentDto(
    Guid Id,
    Guid SessionId,
    string? PauseReason,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc
);
