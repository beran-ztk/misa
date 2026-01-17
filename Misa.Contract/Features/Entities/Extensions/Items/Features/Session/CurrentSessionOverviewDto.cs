namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

public class CurrentSessionOverviewDto
{
    public SessionResolvedDto? ActiveSession { get; set; }
    public SessionResolvedDto? LatestClosedSession { get; set; }

    public bool CanStartSession { get; set; }
    public bool CanPauseSession { get; set; }
    public bool CanContinueSession { get; set; }
    public bool CanStopSession { get; set; }
    
    public TimeSpan? ElapsedTime { get; set; }
    public int ItemSessionCount { get; set; }
    public double? AvgEfficiency { get; set; }
    public double? AvgConcentration { get; set; }
    public double? AvgAccuracyOfPlannedDuration { get; set; }
}