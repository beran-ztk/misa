namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

public class SessionResolvedDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public SessionStateDto State { get; set; }
    public EfficiencyContract Efficiency { get; set; }
    public ConcentrationContract Concentration { get; set; }

    public string? Objective { get; set; }
    public string? Summary { get; set; }
    public string? AutoStopReason { get; set; }

    public TimeSpan? PlannedDuration { get; set; }

    public bool StopAutomatically { get; set; }
    public bool? WasAutomaticallyStopped { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public required ICollection<SessionSegmentDto> Segments { get; set; }
    
    public string? ElapsedTime { get; set; }

}