namespace Misa.Contract.Audit.Session;

public record PauseSessionDto(
    Guid ItemId,
    string? PauseReason
);
