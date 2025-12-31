namespace Misa.Domain.Scheduling;

public sealed class ScheduledDeadline
{
    private ScheduledDeadline() { } // EF Core

    public ScheduledDeadline(Guid itemId, DateTimeOffset deadlineAtUtc)
    {
        if (itemId == Guid.Empty)
            throw new ArgumentException("ItemId must not be empty", nameof(itemId));
        
        ItemId = itemId;
        DeadlineAtUtc = deadlineAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; } // DB PK
    public Guid ItemId { get; private set; }

    public DateTimeOffset DeadlineAtUtc { get; private set; }

    public void Reschedule(DateTimeOffset deadlineAtUtc)
        => DeadlineAtUtc = deadlineAtUtc.ToUniversalTime();
}