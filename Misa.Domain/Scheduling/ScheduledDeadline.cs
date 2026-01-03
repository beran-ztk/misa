using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Dictionaries.Audit;

namespace Misa.Domain.Scheduling;

public sealed class ScheduledDeadline : DomainEventEntity
{
    private ScheduledDeadline() { } // EF Core
    public ScheduledDeadline(Guid itemId, DateTimeOffset deadlineAtUtc)
    {
        if (itemId == Guid.Empty)
            throw new ArgumentException("ItemId must not be empty", nameof(itemId));
        
        ItemId = itemId;
        
        Reschedule(deadlineAtUtc);
    }

    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }

    public DateTimeOffset DeadlineAtUtc { get; private set; }

    public void Reschedule(DateTimeOffset deadlineAtUtc)
    {
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: ItemId,
            ActionType: (int)ActionTypes.Deadline,
            OldValue: DeadlineAtUtc == default ? null : DeadlineAtUtc.ToString(),
            NewValue: deadlineAtUtc.ToString(),
            Reason: null
        ));
        
        DeadlineAtUtc = deadlineAtUtc;
    }
    public void Remove()
    {
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: ItemId,
            ActionType: (int)ActionTypes.Deadline,
            OldValue: DeadlineAtUtc.ToString(),
            NewValue: null,
            Reason: null
        ));
    }
}