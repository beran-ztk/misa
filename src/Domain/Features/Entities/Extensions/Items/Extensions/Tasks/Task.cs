using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;

public class Task
{
    private Task() {}
    private Task(Item item, TaskCategory category)
    {
        Item = item;
        Category = category;
    }
    public static Task Create(Guid id, string title, TaskCategory category, Priority priority, DateTimeOffset createdAt)
    {
        var item = Item.Create(id, Workflow.Task, title, priority, createdAt);
        return new Task(item, category);
    }
    public Guid Id { get; init; }
    public TaskCategory Category { get; private set; }
    
    public Item Item { get; private set; }
}