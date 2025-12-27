using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Scheduling;

namespace Misa.Application.Scheduling.Commands.SetEntityDeadline;

public class SetEntityDeadlineHandler(IScheduleRepository repository)
{
    public async Task Handle(SetEntityDeadlineCommand command)
    {
        var schedule = new Schedule(command.EntityId, command.DueAtUtc, null);
        await repository.Upsert(schedule);
    }
}