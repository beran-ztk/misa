namespace Misa.Contract.Audit;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public int StateId { get; set; }
    public int? EfficiencyId { get; set; } 
    public int? ConcentrationId { get; set; }

    public Lookups.SessionStateDto State { get; set; } = new();
    public Lookups.SessionEfficiencyTypeDto? Efficiency { get; set; }
    public Lookups.SessionConcentrationTypeDto? Concentration { get; set; }

    public string? Objective { get; set; }
    public string? Summary { get; set; }
    public string? AutoStopReason { get; set; }

    public TimeSpan? PlannedDuration { get; set; }

    public bool StopAutomatically { get; set; }
    public bool? WasAutomaticallyStopped { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<SessionSegmentDto> Segments { get; set; } = new();
}