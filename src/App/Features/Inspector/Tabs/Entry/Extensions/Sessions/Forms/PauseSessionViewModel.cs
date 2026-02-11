using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public partial class PauseSessionViewModel(Guid itemId) : ViewModelBase, IHostedForm<PauseSessionDto>
{
    // Host
    public string Title => "Pause session";
    public string SubmitText => "Save";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    public PauseSessionDto TrySubmit() => Submit();

    // Content
    [ObservableProperty] private string? _pauseReason;

    private PauseSessionDto Submit() => new(itemId, PauseReason);
}