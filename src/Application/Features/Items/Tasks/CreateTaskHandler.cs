using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;
using Misa.Domain.Items;

namespace Misa.Application.Features.Items.Tasks;
public sealed record CreateTaskCommand(
    string Title,
    string Description,
    TaskCategoryDto CategoryDto,
    ActivityPriorityDto ActivityPriorityDto,
    DateTimeOffset? DueDate
);
public class CreateTaskHandler(
    IItemRepository repository,
    ITimeProvider timeProvider, 
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task<TaskDto> HandleAsync(CreateTaskCommand command, CancellationToken ct)
    {
        var task = Item.CreateTask(
            new ItemId(idGenerator.New()), 
            ownerId: currentUser.Id,
            command.Title, 
            command.Description,
            command.CategoryDto.ToDomain(), 
            timeProvider.UtcNow,
            command.ActivityPriorityDto.ToDomain(),
            command.DueDate
        );
        
        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);

        var formattedTask = task.ToTaskExtensionDto();
        
        return formattedTask;
    }
}