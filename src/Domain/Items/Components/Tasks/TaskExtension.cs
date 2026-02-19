namespace Misa.Domain.Items.Components.Tasks;

public sealed class TaskExtension
{
    private TaskExtension() {} // EF
    public TaskExtension(ItemId id, TaskCategory category)
    {
        Id = id;
        Category = category;
    }
    public ItemId Id { get; init; }
    public TaskCategory Category { get; private set; }
}