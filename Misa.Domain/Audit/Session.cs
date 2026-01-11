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

    public SessionSegment? GetLatestSegment()
    {
        return Segments.Count == 0
            ? new SessionSegment()
            : Segments.MaxBy(s => s.StartedAtUtc);
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