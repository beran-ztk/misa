using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Scheduling;

namespace Misa.Application.Scheduling.Commands.SetEntityDeadline;

public class SetEntityDeadlineHandler(IScheduleRepository repository)
{
    public async Task Handle(SetEntityDeadlineCommand command)
    {
        await repository.Upsert(command.EntityIdOfItem, command.DueAtUtc);
    }
}