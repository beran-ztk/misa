namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

public record PauseSessionDto(
    Guid ItemId,
    string? PauseReason
);
