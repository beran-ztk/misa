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
    public static Task Create() => new();
    public static Task Create(string title, TaskCategory category, Priority priority)
    {
        var item = Item.Create(Workflow.Task, title, priority);
        return new Task(item, category);
    }
    public Guid Id { get; private set; }
    public TaskCategory Category { get; private set; }
    
    public Item Item { get; private set; }
}