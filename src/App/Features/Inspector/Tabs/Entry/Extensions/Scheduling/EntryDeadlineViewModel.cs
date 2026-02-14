using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Common.Deadlines;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Scheduling.Forms;
using Misa.Ui.Avalonia.Infrastructure.UI;
using ReactiveUI;

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
    private async Task ShowUpsertDeadlinePanelAsync()
    {
        var itemId = Facade.State.Item.Id;

        var formVm = new UpsertDeadlineViewModel(itemId, Deadline, Facade.Gateway);

        var result = await Facade.PanelProxy.OpenAsync<DeadlineDto>(PanelKey.UpsertDeadline, formVm);

        if (result is null) return;

        Facade.State.Deadline = result;
    }

    [RelayCommand]
    private async Task DeleteDeadlineAsync(CancellationToken ct)
    {
        var itemId = Facade.State.Item.Id;

        await Facade.Gateway.DeleteDeadlineAsync(itemId);
        await Facade.Reload();
    }
}