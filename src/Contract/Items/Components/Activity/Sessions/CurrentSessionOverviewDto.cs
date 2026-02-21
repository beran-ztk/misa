namespace Misa.Contract.Items.Components.Activity.Sessions;

public class CurrentSessionOverviewDto
{
    public SessionDto? ActiveSession { get; set; }
    public SessionDto? LatestClosedSession { get; set; }
    
    public TimeSpan? ElapsedTime { get; set; }
    public int ItemSessionCount { get; set; }
    public double? AvgEfficiency { get; set; }
    public double? AvgConcentration { get; set; }
    public double? AvgAccuracyOfPlannedDuration { get; set; }
}