namespace Misa.Contract.Audit;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public int? EfficiencyId { get; set; } 
    public int? ConcentrationId { get; set; } 

    public Lookups.SessionEfficiencyTypeDto? Efficiency { get; set; } = new();
    public Lookups.SessionConcentrationTypeDto? Concentration { get; set; }

    public string? Objective { get; set; }
    public string? Summary { get; set; }
    public string? AutoStopReason { get; set; }

    public TimeSpan? PlannedDuration { get; set; }
    public TimeSpan? ActualDuration { get; set; }

    public bool StopAutomatically { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
}