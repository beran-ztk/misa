using Misa.Domain.Exceptions;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Common;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Domain.Shared.DomainEvents;

namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public class Item : DomainEventEntity
{
    private Item() { }

    public Item(Entity entity, string title, Priority priority, ItemState state)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException("title", "title_required", "Title is required.");

        Entity = entity;
        Title = title;
        Priority = priority;
        State = state;
    }
    public static Item Create(Guid id, string ownerId, Workflow workflow, string title, Priority priority, DateTimeOffset createdAtUtc)
    {
        var entity = Entity.Create(id, ownerId, workflow, createdAtUtc);
        return new Item(entity, title, priority, ItemState.Draft);
    }
    // Member
    public Guid Id { get; init; }
    public ItemState State { get; private set; }
    public Priority Priority { get; private set; }
    public string Title { get; private set; }
    
    // Modelle
    public Entity Entity { get; init; }
    public Deadline? Deadline { get; private set; }
    
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
     public bool HasActiveSession 
        => State == ItemState.Active;
    public bool CanStartSession
        => State 
            is ItemState.Draft
            or ItemState.Undefined
            or ItemState.InProgress
            or ItemState.Pending
            or ItemState.WaitForResponse;

    public void ChangeState(ItemState state)
    {
        if (State == state)
            return;
        
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: Id,
            ChangeType: ChangeType.State,
            OldValue: State.ToString(),
            NewValue: state.ToString(),
            Reason: null
        ));
        State = state;
    }
}