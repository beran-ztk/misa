namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

public class Session
{
    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }
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
    public ICollection<SessionSegment> Segments { get; init; } = [];

    public TimeSpan? ElapsedTime(DateTimeOffset utcNow) =>
        Segments.Aggregate(TimeSpan.Zero, (sum, s) =>
        {
            var end = s.EndedAtUtc ?? utcNow;
            return sum + (end - s.StartedAtUtc);
        });
    
    public string? FormattedElapsedTime(DateTimeOffset utcNow) =>
        ElapsedTime(utcNow) switch
        {
            null => null,

            { TotalDays: >= 1 } ts
                => $"{(int)ts.TotalDays}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00} d",

            { TotalHours: >= 1 and < 24 } ts
                => $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00} h",

            { TotalMinutes: >= 1 and < 60 } ts
                => $"{(int)ts.TotalMinutes}:{ts.Seconds:00} min",

            _ => null
        };

    public void Autostop()
    {
        WasAutomaticallyStopped = true;
    }
    public SessionSegment? GetLatestActiveSegment() 
        => Segments
            .Where(s => s.EndedAtUtc == null)
            .MaxBy(s => s.StartedAtUtc);

    public void AddStartSegment()
    {
        var segment = new SessionSegment(Id, CreatedAtUtc);
        Segments.Add(segment);
    }
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

    public void Continue(DateTimeOffset startedAtUtc)
    {
        if (State != SessionState.Paused)
            throw new InvalidOperationException("Session is not paused.");
        
        State = SessionState.Running;
        
        var segment = new SessionSegment(Id, startedAtUtc);
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
        Guid entityId, 
        TimeSpan? plannedDuration, 
        string? objective,
        bool stopAutomatically, 
        string? autoStopReason, 
        DateTimeOffset nowUtc) => new()
    {
        ItemId = entityId,
        PlannedDuration = plannedDuration,
        Objective = objective,
        StopAutomatically = stopAutomatically,
        AutoStopReason = autoStopReason,
        State = SessionState.Running,
        CreatedAtUtc = nowUtc
    };
}