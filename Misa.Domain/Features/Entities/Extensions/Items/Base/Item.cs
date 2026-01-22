using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public class Item : DomainEventEntity
{
    private Item() { }

    public Item(
        int stateId,
        Priority priority,
        string title)
    {
        StateId = stateId;
        Priority = priority;
        Title = title ?? throw new ArgumentNullException(nameof(title));
    }
    // Member
    public Guid Id { get; set; }
    public int StateId { get; private set; }
    public Priority Priority { get; private set; }
    public string Title { get; private set; }
    
    // Modelle
    public Entity Entity { get; set; }
    public State State { get; private set; }
    
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
            EntityId: Id,
            ChangeType: ChangeType.State,
            OldValue: StateId.ToString(),
            NewValue: value.ToString(),
            Reason: null
        ));
        StateId = value;
    }
}