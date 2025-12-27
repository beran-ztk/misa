namespace Misa.Application.Scheduling.Commands.SetEntityDeadline;

public record SetEntityDeadlineCommand(Guid EntityId, DateTimeOffset DueAtUtc);
