namespace Misa.Application.Items.Features.Sessions.Commands;

public record PauseSessionCommand(
    Guid ItemId,
    string? PauseReason
);
