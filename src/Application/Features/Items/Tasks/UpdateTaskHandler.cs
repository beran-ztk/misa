using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Core.Features.Items.Tasks;

public record UpdateTaskCommand(
    Guid ItemId,
    string? Title,
    string? Description,
    ActivityState? ActivityState,
    ActivityPriority? ActivityPriority,
    TaskCategory? TaskCategory,
    string? Reason = null);

public sealed class UpdateTaskHandler(ItemRepository repository)
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
            item.Activity.ChangeState(state, command.Reason);
        if (command.ActivityPriority is { } priority)
            item.Activity.ChangePriority(priority);
        if (command.TaskCategory is { } category)
            item.ChangeTaskCategory(category);

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
