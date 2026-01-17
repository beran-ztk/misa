namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;

public record PauseSessionCommand(
    Guid ItemId,
    string? PauseReason
);
