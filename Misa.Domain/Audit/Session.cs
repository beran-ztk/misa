namespace Misa.Domain.Audit;

public class Session
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }

    public int? EfficiencyId { get; private set; }
    public SessionEfficiencyType? Efficiency { get; private set; }

    public int? ConcentrationId { get; private set; }
    public SessionConcentrationType? Concentration { get; private set; }

    public string? Objective { get; private set; }
    public string? Summary { get; private set; }
    public string? AutoStopReason { get; private set; }

    public TimeSpan? PlannedDuration { get; private set; }

    public TimeSpan? ActualDuration => EndedAtUtc - StartedAtUtc;
    public bool StopAutomatically { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    public static Session Start(
        Guid entityId, TimeSpan? plannedDuration, string? objective,
        bool stopAutomatically, string? autoStopReason, DateTimeOffset nowUtc
    )
        => new()
        {
            EntityId = entityId,
            PlannedDuration = plannedDuration,
            Objective = objective,
            StopAutomatically = stopAutomatically,
            AutoStopReason = autoStopReason,
            StartedAtUtc = nowUtc,
            EndedAtUtc = null
        };

    public void Pause(int? efficiencyId, int? concentrationId, string? summary, DateTimeOffset ended)
    {
        EfficiencyId = efficiencyId;
        ConcentrationId = concentrationId;
        Summary = summary;
        EndedAtUtc = ended;
    }
}