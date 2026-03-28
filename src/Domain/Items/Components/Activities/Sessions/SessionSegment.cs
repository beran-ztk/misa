namespace Misa.Domain.Items.Components.Activities.Sessions;

public class SessionSegment
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }

    public string? PauseReason { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    public SessionSegment() { }
    private SessionSegment(DateTimeOffset startedAtUtc)
    {
        Id = Guid.NewGuid();
        StartedAtUtc = startedAtUtc;
    }
    public static SessionSegment Create() => new SessionSegment(DateTimeOffset.UtcNow);
    public void End(string? pauseReason)
    {
        if (EndedAtUtc != null)
            throw new InvalidOperationException("Segment already ended.");

        EndedAtUtc = DateTimeOffset.UtcNow;
        PauseReason = pauseReason;
    }
}