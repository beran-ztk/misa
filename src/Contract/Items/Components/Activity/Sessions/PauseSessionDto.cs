namespace Misa.Contract.Items.Components.Activity.Sessions;

public record PauseSessionDto(
    Guid ItemId,
    string? PauseReason
);
