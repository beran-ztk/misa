using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.MarkAsDone;

public sealed partial class MarkItemDoneViewModel(Guid itemId)
    : ViewModelBase, IHostedForm<Result>
{
    [ObservableProperty] private string? _reason;

    public string FormTitle { get; } = "Mark as Done";
    public string? FormDescription { get; } = "Optionally add a reason or note for completing this item.";

    public async Task<Result<Result>> SubmitAsync()
    {
        var request = new ChangeActivityStateRequest(ActivityStateDto.Done, Reason);
        // var result = await gateway.ChangeActivityStateAsync(itemId, request);

        // if (!result.IsSuccess)
            return Result<Result>.Failure();

        return Result<Result>.Ok(Result.Ok());
    }
}
