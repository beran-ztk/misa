using Misa.Contract.Audit.Lookups;
using Misa.Contract.Audit.Session;

namespace Misa.Contract.Items.Details;

public class CurrentSessionOverviewDto
{
    public SessionResolvedDto? ActiveSession { get; set; }
    public SessionResolvedDto? LatestClosedSession { get; set; }

    public bool CanStartSession { get; set; }
    public bool CanPauseSession { get; set; }
    public bool CanContinueSession { get; set; }
    public bool CanStopSession { get; set; }
    
    public int ItemSessionCount { get; set; }
    public double? AvgEfficiency { get; set; }
    public double? AvgConcentration { get; set; }
    public double? AvgAccuracyOfPlannedDuration { get; set; }
}