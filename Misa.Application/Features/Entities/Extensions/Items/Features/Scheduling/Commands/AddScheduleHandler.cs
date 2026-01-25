using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public record AddScheduleCommand(
    string Title,
    ScheduleFrequencyTypeContract ScheduleFrequencyType,
    int FrequencyInterval
);

public class AddScheduleHandler(IItemRepository repository)
{
    public async Task<Result> Handle(AddScheduleCommand command, CancellationToken ct)
    {
        try
        {
            // Validate FrequencyInterval
            if (command.FrequencyInterval <= 0)
            {
                return Result.Invalid("FrequencyInterval", "Frequency interval must be greater than 0");
            }

            // Map contract enum to domain enum
            var frequencyType = command.ScheduleFrequencyType.MapToDomain();

            // Create Scheduler using domain Create method
            var scheduler = Scheduler.Create(
                command.Title,
                frequencyType,
                command.FrequencyInterval
            );

            // Persist via repository
            await repository.AddAsync(scheduler, ct);
            await repository.SaveChangesAsync(ct);

            return Result.Ok();
        }
        catch (ArgumentException ex)
        {
            return Result.Invalid("", ex.Message);
        }
    }
}