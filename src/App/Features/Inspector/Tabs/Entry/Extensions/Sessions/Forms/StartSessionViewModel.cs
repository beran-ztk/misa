using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public sealed partial class StartSessionViewModel(Guid itemId, InspectorGateway gateway)
    : ViewModelBase, IHostedForm<SessionDto>
{
    // Host
    public string Title => "Start a Session";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    // Content
    [ObservableProperty] private int? _plannedMinutes;
    [ObservableProperty] private string? _objective;
    [ObservableProperty] private bool _stopAutomatically;
    [ObservableProperty] private string? _autoStopReason;

    public async Task<Result<SessionDto>> SubmitAsync()
    {
        TimeSpan? plannedDuration = PlannedMinutes.HasValue
            ? TimeSpan.FromMinutes(Convert.ToInt32(PlannedMinutes.Value))
            : null;

        var dto = new StartSessionDto(
            itemId,
            plannedDuration,
            Objective,
            StopAutomatically,
            AutoStopReason
        );
        
        return await gateway.StartSessionAsync(dto);
    }
}