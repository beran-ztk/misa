using Misa.Domain.Dictionaries.Audit;

namespace Misa.Domain.Audit;

public class Session
{
    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }

    public int StateId { get; set; } = 1;
    public SessionStates State { get; private set; }
    public void PauseSession() 
        => StateId = (int)SessionState.Paused;
    public void ContinueSession() 
        => StateId = (int)SessionState.Running;
    public int? EfficiencyId { get; set; }
    public SessionEfficiencyType? Efficiency { get; private set; }

    public int? ConcentrationId { get; set; }
    public SessionConcentrationType? Concentration { get; private set; }

    public string? Objective { get; private set; }
    public string? Summary { get; set; }
    public string? AutoStopReason { get; private set; }

    public TimeSpan? PlannedDuration { get; private set; }
    
    public bool StopAutomatically { get; private set; }
    public bool? WasAutomaticallyStopped { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public ICollection<SessionSegment> Segments { get; set; } = [];
    
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
        if (StateId != (int)SessionState.Running)
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

        StateId = (int)SessionState.Paused;
    }

    public void Continue(DateTimeOffset startedAtUtc)
    {
        if (StateId != (int)SessionState.Paused)
            throw new InvalidOperationException("Session is not paused.");
        
        StateId = (int)SessionState.Running;
        
        var segment = new SessionSegment(Id, startedAtUtc);
        Segments.Add(segment);
    }
    public void Stop(
        DateTimeOffset nowUtc,
        int? efficiencyId,
        int? concentrationId,
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

        EfficiencyId = efficiencyId;
        ConcentrationId = concentrationId;
        Summary = summary;

        StateId = (int)SessionState.Completed;
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
        StateId = (int)SessionState.Running,
        CreatedAtUtc = nowUtc
    };

}