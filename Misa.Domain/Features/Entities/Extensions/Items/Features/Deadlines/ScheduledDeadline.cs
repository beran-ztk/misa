using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Features.Actions;

namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;

public sealed class ScheduledDeadline : DomainEventEntity
{
    private ScheduledDeadline() { } // EF Core
    public ScheduledDeadline(Guid itemId, DateTimeOffset deadlineAtUtc)
    {
        if (itemId == Guid.Empty)
            throw new ArgumentException("ItemId must not be empty", nameof(itemId));
        
        ItemId = itemId;
        
        RescheduleDeadlineAndAuditChanges(deadlineAtUtc);
    }

    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }

    public DateTimeOffset DeadlineAtUtc { get; private set; }

    public void RescheduleDeadlineAndAuditChanges(DateTimeOffset? deadlineAtUtc)
    {
        AddDomainEvent(new PropertyChangedEvent(
            EntityId: ItemId,
            ActionType: (int)ActionTypes.Deadline,
            OldValue: DeadlineAtUtc == default ? null : DeadlineAtUtc.ToString(),
            NewValue: deadlineAtUtc.ToString(),
            Reason: null
        ));

        if (deadlineAtUtc is not null)
        {
            DeadlineAtUtc = (DateTimeOffset)deadlineAtUtc;   
        }
    }
    public void RemoveDeadlineAndAuditChanges()
    {
        RescheduleDeadlineAndAuditChanges(null);
    }
}