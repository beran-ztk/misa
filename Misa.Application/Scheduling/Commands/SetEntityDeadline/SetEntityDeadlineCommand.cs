namespace Misa.Application.Scheduling.Commands.SetEntityDeadline;

public record SetEntityDeadlineCommand(Guid EntityIdOfItem, DateTimeOffset DueAtUtc);
