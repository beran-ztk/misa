namespace Misa.Domain.Scheduling;

public sealed class Schedule
{
    private Schedule() {} // EF Core
    public Schedule(Guid entityId, DateTimeOffset startAtUtc, DateTimeOffset? endAtUtc)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("EntityId must not be empty", nameof(entityId));

        ValidateScheduleWindow(startAtUtc, endAtUtc);
        
        EntityId = entityId;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
    }
    
    public Guid EntityId { get; private set; }
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset? EndAtUtc { get; private set; }

    public bool HasTimeSpan => EndAtUtc.HasValue;
    
    public void Reschedule(DateTimeOffset startAtUtc, DateTimeOffset? endAtUtc)
    {
        ValidateScheduleWindow(startAtUtc, endAtUtc);

        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
    }
    
    private static void ValidateScheduleWindow(DateTimeOffset startAtUtc, DateTimeOffset? endAtUtc)
    {
        if (endAtUtc.HasValue && endAtUtc.Value <= startAtUtc)
            throw new ArgumentException("EndAtUtc must be greater than StartAtUtc.", nameof(endAtUtc));
    }
}