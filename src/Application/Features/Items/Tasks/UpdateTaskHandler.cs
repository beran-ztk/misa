using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;
using Misa.Domain.Exceptions;
using Misa.Application.Mappings;

namespace Misa.Application.Features.Items.Tasks;

public record UpdateTaskCommand(
    Guid ItemId,
    string? Title,
    string? Description,
    ActivityStateDto? ActivityState,
    ActivityPriorityDto? ActivityPriority,
    TaskCategoryDto? TaskCategory,
    string? Reason = null);

public sealed class UpdateTaskHandler(IItemRepository repository)
{
    public async Task HandleAsync(UpdateTaskCommand command)
    {
        var item = await repository.TryGetTaskAsync(command.ItemId, CancellationToken.None);
        if (item?.Activity is null || item.TaskExtension is null)
            throw new DomainNotFoundException("task.not.found", command.ItemId.ToString());

        if (!string.IsNullOrEmpty(command.Title))
            item.ChangeTitle(command.Title);
        if (command.Description is not null)
            item.ChangeDescription(command.Description);
        if (command.ActivityState is { } state)
            item.Activity.ChangeState(state.ToDomain(), command.Reason);
        if (command.ActivityPriority is { } priority)
            item.Activity.ChangePriority(priority.ToDomain());
        if (command.TaskCategory is { } category)
            item.TaskExtension.ChangeCategory(category.ToDomain());

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
