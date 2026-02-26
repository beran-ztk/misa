using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public partial class PauseSessionViewModel(Guid itemId, InspectorGateway gateway) : ViewModelBase, IHostedForm
{
    // Host
    public string Title => "Pause session";
    public string SubmitText => "Save";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;


    // Content
    [ObservableProperty] private string? _pauseReason;

    public async Task<Result> SubmitAsync()
    {

        var dto = new PauseSessionRequest(
            PauseReason
        );
        
        return await gateway.PauseSessionAsync(itemId, dto);
    }
}