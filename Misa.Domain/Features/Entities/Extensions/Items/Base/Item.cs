using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Features.Actions;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public class Item : DomainEventEntity
{
    // Für EF Core
    private Item() { }

    public Item(
        Entity entity,
        int stateId,
        Priority priority,
        int categoryId,
        string title)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));

        StateId = stateId;
        Priority = priority;
        CategoryId = categoryId;
        Title = title ?? throw new ArgumentNullException(nameof(title));
    }
    // Member
    public Guid EntityId { get; set; }
    public int StateId { get; private set; }
    public Priority Priority { get; private set; }
    public int CategoryId { get; private set; }
    public string Title { get; private set; }
    
    // Modelle
    public Entity Entity { get; set; }
    public State State { get; private set; }
    public Category Category { get; private set; }
    
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
    public ScheduledDeadline? ScheduledDeadline { get; private set; }

    public void ChangeState(ItemStates state)
    {
        var value = (int)state;
        if (StateId == value)
            return;
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.State,
            OldValue: StateId.ToString(),
            NewValue: value.ToString(),
            Reason: null
        ));
        StateId = value;
    }
}