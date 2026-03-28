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
    public bool PlannedDurationNotificationSent { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private init; }
    
    // Components
    public ICollection<SessionSegment> Segments { get; init; } = [];
    public Item Item { get; set; } = null!;
    
    // State Change
    public void Autostop()
    {
        WasAutomaticallyStopped = true;
    }

    public void MarkPlannedDurationNotificationSent()
    {
        PlannedDurationNotificationSent = true;
    }

    // Behaviour
    public void Pause(string? pauseReason)
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

        State = SessionState.Paused;

        var segment = openSegments[0];
        segment.End(pauseReason);
    }

    public void Continue()
    {
        if (State != SessionState.Paused)
            throw new InvalidOperationException("Session is not paused.");

        State = SessionState.Running;
        Segments.Add(SessionSegment.Create());
    }
    public void Stop(
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
                openSegments[0].End(null);
                break;
        }

        Efficiency = efficiency;
        Concentration = concentration;
        Summary = summary;

        State = SessionState.Ended;
    }
    public static Session Start(
        TimeSpan? plannedDuration,
        string? objective,
        bool stopAutomatically,
        string? autoStopReason)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            PlannedDuration = plannedDuration,
            Objective = objective,
            StopAutomatically = stopAutomatically,
            AutoStopReason = autoStopReason,
            State = SessionState.Running,
            CreatedAtUtc = now
        };

        session.Segments.Add(SessionSegment.Create());

        return session;
    }
    
    
    public TimeSpan TotalElapsed => TimeSpan.FromTicks(
        Segments
            .Where(s => s.EndedAtUtc is not null)
            .Sum(s => (s.EndedAtUtc!.Value - s.StartedAtUtc).Ticks));

    public string DurationDisplay
    {
        get
        {
            var elapsed = TotalElapsed;
            if (elapsed == TimeSpan.Zero)
                return PlannedDuration.HasValue
                    ? $"0 / {(int)PlannedDuration.Value.TotalMinutes}m"
                    : "0m";

            var elapsedStr = elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m"
                : $"{(int)elapsed.TotalMinutes}m";

            if (PlannedDuration is null)
                return elapsedStr;

            var plannedStr = PlannedDuration.Value.TotalHours >= 1
                ? $"{(int)PlannedDuration.Value.TotalHours}h {PlannedDuration.Value.Minutes}m"
                : $"{(int)PlannedDuration.Value.TotalMinutes}m";

            return $"{elapsedStr} / {plannedStr}";
        }
    }

    public string? EfficiencyDisplay => Efficiency == SessionEfficiencyType.None ? null : Efficiency switch
    {
        SessionEfficiencyType.LowOutput      => "Low output",
        SessionEfficiencyType.SteadyOutput   => "Steady output",
        SessionEfficiencyType.HighOutput     => "High output",
        SessionEfficiencyType.PeakPerformance => "Peak performance",
        _ => null
    };

    public string? ConcentrationDisplay => Concentration == SessionConcentrationType.None ? null : Concentration switch
    {
        SessionConcentrationType.Distracted      => "Distracted",
        SessionConcentrationType.UnfocusedButCalm => "Unfocused but calm",
        SessionConcentrationType.Focused         => "Focused",
        SessionConcentrationType.DeepFocus       => "Deep focus",
        SessionConcentrationType.Hyperfocus      => "Hyperfocus",
        _ => null
    };
}