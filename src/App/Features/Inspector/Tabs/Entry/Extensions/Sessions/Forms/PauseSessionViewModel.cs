using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public partial class PauseSessionViewModel(Guid itemId, InspectorGateway gateway) : ViewModelBase, IHostedForm<SessionDto>
{
    // Host
    public string Title => "Pause session";
    public string SubmitText => "Save";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;


    // Content
    [ObservableProperty] private string? _pauseReason;

    public async Task<Result<SessionDto>> SubmitAsync()
    {

        var dto = new PauseSessionDto(
            itemId,
            PauseReason
        );
        
        return await gateway.PauseSessionAsync(dto);
    }
}