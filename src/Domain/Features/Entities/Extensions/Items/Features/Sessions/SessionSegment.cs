namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

public class SessionSegment
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }

    public string? PauseReason { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    public SessionSegment() { }
    public SessionSegment(Guid sessionId, DateTimeOffset startedAtUtc)
    {
        SessionId = sessionId;
        StartedAtUtc = startedAtUtc;
    }

    public void CloseSegment(string? pauseReason, DateTimeOffset? endedAtUtc)
    {
        PauseReason = pauseReason;
        EndedAtUtc = endedAtUtc ?? DateTimeOffset.UtcNow;
    }
    public void End(DateTimeOffset endedAtUtc, string? pauseReason)
    {
        if (EndedAtUtc != null)
            throw new InvalidOperationException("Segment already ended.");

        EndedAtUtc = endedAtUtc;
        PauseReason = pauseReason;
    }

}