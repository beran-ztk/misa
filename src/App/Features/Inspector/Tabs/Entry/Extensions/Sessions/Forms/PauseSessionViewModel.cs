using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public partial class PauseSessionViewModel(Guid itemId, InspectorGateway gateway)
    : ViewModelBase, IHostedForm<Result>
{
    // Content
    [ObservableProperty] private string? _pauseReason;

    public string FormTitle { get; } = "Pause Session";
    public string? FormDescription { get; }

    public async Task<Result<Result>> SubmitAsync()
    {
        var dto = new PauseSessionRequest(
            PauseReason
        );
        
        var result = await gateway.PauseSessionAsync(itemId, dto);

        if (!result.IsSuccess)
            return Result<Result>.Failure();

        return Result<Result>.Ok(Result.Ok());
    }
}