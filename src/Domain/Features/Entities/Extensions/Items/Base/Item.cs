using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Domain.Shared.DomainEvents;

namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public class Item : DomainEventEntity
{
    private Item() { }

    public Item(Entity entity, string title, Priority priority, ItemStates state)
    {
        Entity = entity;
        Title = title;
        Priority = priority;
        StateId = (int)state;
    }
    public static Item Create(Workflow workflow, string title, Priority priority, DateTimeOffset createdAtUtc)
    {
        var entity = Entity.Create(workflow, createdAtUtc);
        return new Item(entity, title, priority, ItemStates.Draft);
    }
    // Member
    public Guid Id { get; init; }
    public int StateId { get; private set; }
    public Priority Priority { get; private set; }
    public string Title { get; private set; }
    
    // Modelle
    public Entity Entity { get; init; }
    public State? State { get; private set; }
    
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
     public bool HasActiveSession 
        => StateId == (int)ItemStates.Active;
    public bool CanStartSession
        => StateId 
            is (int)ItemStates.Draft
            or (int)ItemStates.Undefined
            or (int)ItemStates.InProgress
            or (int)ItemStates.Pending
            or (int)ItemStates.WaitForResponse;

    public void ChangeState(ItemStates state)
    {
        var value = (int)state;
        if (StateId == value)
            return;
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: Id,
            ChangeType: ChangeType.State,
            OldValue: StateId.ToString(),
            NewValue: value.ToString(),
            Reason: null
        ));
        StateId = value;
    }
}