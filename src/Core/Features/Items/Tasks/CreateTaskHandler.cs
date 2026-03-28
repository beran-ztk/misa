using Misa.Core.Common.Abstractions.Persistence;
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
public class CreateTaskHandler(IItemRepository repository)
{
    public async Task HandleAsync(CreateTaskCommand command, CancellationToken ct)
    {
        var task = Item.CreateTask(
            new ItemId(Guid.NewGuid()),
            command.Title, 
            command.Description,
            command.CategoryDto, 
            DateTimeOffset.UtcNow,
            command.ActivityPriorityDto,
            command.DueDate
        );
        
        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);
    }
}