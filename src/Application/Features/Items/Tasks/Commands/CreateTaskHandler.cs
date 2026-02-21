using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Domain.Items;

namespace Misa.Application.Features.Items.Tasks.Commands;
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
    public async Task<Result<TaskExtensionDto>> HandleAsync(CreateTaskCommand command, CancellationToken ct)
    {
        var task = CreateTaskCommandToDomain(command);
        
        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);

        var formattedTask = task.ToTaskExtensionDto();
        
        return Result<TaskExtensionDto>
            .Ok(formattedTask);
    }

    private Item CreateTaskCommandToDomain(CreateTaskCommand command)
    {
        return Item.CreateTask(
            new ItemId(idGenerator.New()), 
            ownerId: currentUser.UserId,
            command.Title, 
            command.Description,
            command.CategoryDto.ToDomain(), 
            timeProvider.UtcNow,
            command.ActivityPriorityDto.ToDomain(),
            command.DueDate
        );
    }
}