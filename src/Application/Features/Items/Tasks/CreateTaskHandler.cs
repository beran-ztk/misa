using Misa.Core.Persistence;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Core.Features.Items.Tasks;
public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    TaskCategory CategoryDto,
    ActivityPriority ActivityPriorityDto,
    DateTimeOffset? DueDate
);
public class CreateTaskHandler(ItemRepository repository)
{
    public async Task HandleAsync(CreateTaskCommand command, CancellationToken ct)
    {
        var task = Item.CreateTask(
            command.Title,
            command.Description,
            command.CategoryDto,
            command.ActivityPriorityDto,
            command.DueDate
        );
        
        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);
    }
}