namespace Misa.Application.Scheduling.Commands.Deadlines;

public record UpsertItemDeadlineCommand(Guid ItemId, DateTimeOffset DueAtUtc);