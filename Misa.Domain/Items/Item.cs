using Misa.Domain.Entities;

namespace Misa.Domain.Items;

public class Item
{
    // Für EF Core
    private Item() { }

    public Item(
        Entity entity,
        int stateId,
        int priorityId,
        int categoryId,
        string title)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));

        StateId = stateId;
        PriorityId = priorityId;
        CategoryId = categoryId;
        Title = title ?? throw new ArgumentNullException(nameof(title));
    }
    // Member
    public Guid EntityId { get; set; }
    public int StateId { get; private set; }
    public int PriorityId { get; private set; }
    public int CategoryId { get; private set; }
    public string Title { get; private set; }
    
    // Modelle
    public Entity Entity { get; private set; }
    public State State { get; private set; }
    public Priority Priority { get; private set; }
    public Category Category { get; private set; }
    
    public void Rename(string title)
    {
        Title = string.IsNullOrWhiteSpace(title)
            ? throw new ArgumentException("Title cannot be empty.", nameof(title))
            : title;
    }
}