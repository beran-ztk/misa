using Misa.Contract.Audit;
using Misa.Contract.Audit.Session;
using Misa.Domain.Audit;
using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Dictionaries.Audit;
using Misa.Domain.Dictionaries.Items;
using Misa.Domain.Entities;
using Misa.Domain.Scheduling;

namespace Misa.Domain.Items;

public class Item : DomainEventEntity
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
    public Entity Entity { get; set; }
    public State State { get; private set; }
    public Priority Priority { get; private set; }
    public Category Category { get; private set; }
    
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
     public bool HasActiveSession 
        => StateId == (int)Dictionaries.Items.ItemStates.Active;
    public bool CanStartSession
        => StateId 
            is (int)Dictionaries.Items.ItemStates.Draft
            or (int)Dictionaries.Items.ItemStates.Undefined
            or (int)Dictionaries.Items.ItemStates.InProgress
            or (int)Dictionaries.Items.ItemStates.Pending
            or (int)Dictionaries.Items.ItemStates.WaitForResponse;

    public Session? GetLatestActiveSession() 
        => Sessions
            .Where(s => 
                s.StateId is (int)Dictionaries.Audit.SessionState.Running 
            or (int)Dictionaries.Audit.SessionState.Paused )
            .MaxBy(s => s.CreatedAtUtc);
    public void EndSession(StopSessionDto dto)
    {
        var latestActiveSession = GetLatestActiveSession();
        if (latestActiveSession == null)
            return;

        ChangeState(ItemStates.InProgress);
        latestActiveSession.StateId = (int)Dictionaries.Audit.SessionState.Completed;
        latestActiveSession.EfficiencyId = dto.Efficiency;
        latestActiveSession.ConcentrationId = dto.Concentration;
        latestActiveSession.Summary = dto.Summary;

        var latestActiveSegment = latestActiveSession.GetLatestActiveSegment();
        if (latestActiveSegment == null)
            return;
        latestActiveSegment.CloseSegment(null, DateTimeOffset.UtcNow);
    }
    
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

    public void ChangePriority(int newValue)
    {
        if (PriorityId == newValue)
            return;
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.Priority,
            OldValue: Priority.ToString(),
            NewValue: newValue.ToString(),
            Reason: null
        ));
        PriorityId = newValue;
    }
    public void ChangeCategory(int newValue)
    {
        if (PriorityId == newValue)
            return;
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.Category,
            OldValue: Category.ToString(),
            NewValue: newValue.ToString(),
            Reason: null
        ));
        CategoryId = newValue;
    }
    public void Rename(string newValue)
    {
        if (Title == newValue)
        {
            return;
        }
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: EntityId,
            ActionType: (int)ActionTypes.Title,
            OldValue: Title,
            NewValue: newValue,
            Reason: null
        ));
        Title = newValue;
    }
}