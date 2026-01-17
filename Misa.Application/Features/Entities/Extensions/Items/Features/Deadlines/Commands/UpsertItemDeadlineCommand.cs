namespace Misa.Application.Features.Entities.Extensions.Items.Features.Deadlines.Commands;

public record UpsertItemDeadlineCommand(Guid ItemId, DateTimeOffset DueAt);