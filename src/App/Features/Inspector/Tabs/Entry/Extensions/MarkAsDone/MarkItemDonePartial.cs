using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.MarkAsDone;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    public bool CanMarkAsDone =>
        Facade.State.Item.Activity?.State is ActivityStateDto.Open;

    [RelayCommand]
    private async Task MarkAsDoneAsync()
    {
        var formVm = new MarkItemDoneViewModel(Facade.State.Item.Id, Facade.Gateway);

        var result = await Facade.LayerProxy.OpenAsync<MarkItemDoneViewModel, Result>(formVm);
        if (result is { IsSuccess: true })
        {
            Facade.ContextState.NotifyUpdated();
            await Facade.Reload();
        }
    }
}
