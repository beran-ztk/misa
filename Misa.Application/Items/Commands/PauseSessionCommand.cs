namespace Misa.Application.Items.Commands;

public record PauseSessionCommand(
    Guid ItemId,
    string? PauseReason
);
