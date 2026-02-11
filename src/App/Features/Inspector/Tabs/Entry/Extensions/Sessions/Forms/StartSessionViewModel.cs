using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public partial class StartSessionViewModel(Guid itemId) : ViewModelBase, IHostedForm<StartSessionDto>
{
    // Host
    public string Title => "Start a Session";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    public StartSessionDto TrySubmit() => Submit();
    
    // Content
    [ObservableProperty] private int? _plannedMinutes;
    [ObservableProperty] private string? _objective;
    [ObservableProperty] private bool _stopAutomatically;
    [ObservableProperty] private string? _autoStopReason;

    private StartSessionDto Submit()
    {
        TimeSpan? plannedDuration = PlannedMinutes.HasValue
            ? TimeSpan.FromMinutes(Convert.ToInt32(PlannedMinutes))
            : null;

        return new StartSessionDto(
            itemId,
            plannedDuration,
            Objective,
            StopAutomatically,
            AutoStopReason
        );
    }
}