namespace Misa.Domain.Items.Components.Activities.Sessions;

public sealed class Session
{
    private Session() {} // EF
    
    // Fields & Properties
    public Guid Id { get; init; }
    public ItemId ItemId { get; init; }
    public SessionState State { get; private set; }
    public SessionEfficiencyType Efficiency { get; private set; }
    public SessionConcentrationType Concentration { get; private set; }

    public string? Objective { get; private set; }
    public string? Summary { get; set; }
    public string? AutoStopReason { get; private set; }

    public TimeSpan? PlannedDuration { get; private set; }
    
    public bool StopAutomatically { get; private set; }
    public bool? WasAutomaticallyStopped { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private init; }
    
    // Components
    public ICollection<SessionSegment> Segments { get; init; } = [];

    // Derived Properties
    public TimeSpan? ElapsedTime(DateTimeOffset utcNow) =>
        Segments.Aggregate(TimeSpan.Zero, (sum, s) =>
        {
            var end = s.EndedAtUtc ?? utcNow;
            return sum + (end - s.StartedAtUtc);
        });
    
    // State Change
    public void Autostop()
    {
        WasAutomaticallyStopped = true;
    }

    // Behaviour
    public void Pause(string? pauseReason, DateTimeOffset nowUtc)
    {
        if (State != SessionState.Running)
            throw new InvalidOperationException("Session is not running.");

        var openSegments = Segments.Where(s => s.EndedAtUtc == null).ToList();

        switch (openSegments.Count)
        {
            case 0:
                throw new InvalidOperationException("No active segment found to pause.");
            case > 1:
                throw new InvalidOperationException("Multiple active segments found (data corruption).");
        }

        var segment = openSegments[0];
        segment.End(nowUtc, pauseReason);

        State = SessionState.Paused;
    }

    public void Continue(Guid segmentId, DateTimeOffset startedAtUtc)
    {
        if (State != SessionState.Paused)
            throw new InvalidOperationException("Session is not paused.");
        
        State = SessionState.Running;
        
        var segment = new SessionSegment(segmentId, startedAtUtc);
        Segments.Add(segment);
    }
    public void Stop(
        DateTimeOffset nowUtc,
        SessionEfficiencyType efficiency,
        SessionConcentrationType concentration,
        string? summary)
    {
        var openSegments = Segments.Where(s => s.EndedAtUtc == null).ToList();

        switch (openSegments.Count)
        {
            case > 1:
                return;
            case 1:
                openSegments[0].End(nowUtc, null);
                break;
        }

        Efficiency = efficiency;
        Concentration = concentration;
        Summary = summary;

        State = SessionState.Ended;
    }
    public static Session Start(
        Guid sessionId,
        Guid segmentId,
        TimeSpan? plannedDuration,
        string? objective,
        bool stopAutomatically,
        string? autoStopReason,
        DateTimeOffset createdAtUtc)
    {
        var session = new Session
        {
            Id = sessionId,
            PlannedDuration = plannedDuration,
            Objective = objective,
            StopAutomatically = stopAutomatically,
            AutoStopReason = autoStopReason,
            State = SessionState.Running,
            CreatedAtUtc = createdAtUtc
        };

        session.Segments.Add(
            SessionSegment.StartSessionSegment(segmentId, createdAtUtc));

        return session;
    }
}