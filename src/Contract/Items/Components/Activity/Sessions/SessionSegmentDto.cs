namespace Misa.Contract.Items.Components.Activity.Sessions;

public sealed record SessionSegmentDto(
    Guid Id,
    Guid SessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    string? PauseReason
);