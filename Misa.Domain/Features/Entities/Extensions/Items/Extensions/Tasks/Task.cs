namespace Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;

public class Task
{
    private Task() {}

    public Task(TaskCategory category)
    {
        Category = category;
    }
    
    public Guid Id { get; private set; }
    public TaskCategory Category { get; private set; }
}