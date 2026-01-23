using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
public record AddScheduleCommand;
public class AddScheduleHandler(IScheduleRepository repository)
{
    public async Task Handle(AddScheduleCommand command, CancellationToken ct)
    {
        await repository.AddSchedulingRule(ct);
    }
}