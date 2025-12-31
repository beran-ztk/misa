namespace Misa.Application.Scheduling.Commands.UpsertItemDeadline;

public record UpsertItemDeadlineCommand(Guid ItemId, DateTimeOffset DueAtUtc);