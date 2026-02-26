namespace Misa.Domain.Items.Components.Activities.Sessions;

public class SessionSegment
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }

    public string? PauseReason { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    public SessionSegment() { }
    public SessionSegment(Guid id, DateTimeOffset startedAtUtc)
    {
        Id = id;
        StartedAtUtc = startedAtUtc;
    }
    public static SessionSegment StartSessionSegment(Guid id, DateTimeOffset startedAtUtc)
    {
        return new SessionSegment(id, startedAtUtc);
    }
    public void End(DateTimeOffset endedAtUtc, string? pauseReason)
    {
        if (EndedAtUtc != null)
            throw new InvalidOperationException("Segment already ended.");

        EndedAtUtc = endedAtUtc;
        PauseReason = pauseReason;
    }
}