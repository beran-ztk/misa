namespace Misa.Contract.Audit.Session;

public record SessionSegmentDto(
    Guid Id,
    Guid SessionId,
    string? PauseReason,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc
);
