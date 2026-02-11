using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Scheduling.Forms;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HasDeadline))]
    [NotifyPropertyChangedFor(nameof(DeadlineDisplay))]
    private DateTime? _deadline;
    public bool HasDeadline => Deadline is not null;
    public string DeadlineDisplay
        => Deadline is null
            ? string.Empty
            : Deadline.Value.ToString("ddd, dd MMM yyyy • HH:mm", CultureInfo.CurrentCulture);

    [RelayCommand]
    public async Task ShowUpsertDeadlinePanelAsync()
    {
        var itemId = Facade.State.Item.Id;

        var formVm = new UpsertDeadlineViewModel(itemId, Deadline);

        var dto = await Facade.PanelProxy.OpenAsync<UpsertDeadlineDto>(PanelKey.UpsertDeadline, formVm);
        if (dto is null) return;

        await Facade.Gateway.UpsertDeadlineAsync(dto);
        await Facade.Reload();
    }

    [RelayCommand]
    public async Task DeleteDeadlineAsync(CancellationToken ct)
    {
        var itemId = Facade.State.Item.Id;

        await Facade.Gateway.DeleteDeadlineAsync(itemId);
        await Facade.Reload();
    }
}