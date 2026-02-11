using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
     public CurrentSessionOverviewDto? CurrentSession => Facade.State.CurrentSessionOverview;

    public bool HasActiveSession => CurrentSession?.ActiveSession != null;
    public bool HasLatestClosedSession => CurrentSession?.LatestClosedSession != null;
    
    public string ActiveSessionElapsedDisplay
        => CurrentSession?.ActiveSession?.ElapsedTime ?? string.Empty;

    public string ActiveSessionSegmentDisplay => $"Segment {CurrentSession?.ActiveSession?.Segments.Count}";
}
