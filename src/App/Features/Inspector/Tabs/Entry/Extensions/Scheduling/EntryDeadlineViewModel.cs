using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Components.DeadlinePicker;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    public DeadlinePickerViewModel DeadlinePicker { get; } = new();

    public DateTime? Deadline => Facade.State.Item.Activity?.DueAt?.ToLocalTime().DateTime;
    public bool HasDeadline => Deadline is not null;
    public string DeadlineDisplay
        => Deadline is null
            ? string.Empty
            : Deadline.Value.ToString("ddd, dd MMM yyyy • HH:mm", CultureInfo.CurrentCulture);

    [ObservableProperty] private bool _showEditDeadline;

    [RelayCommand]
    private void ShowEditDeadlineForm()
    {
        ShowEditDeadline = !ShowEditDeadline;

        if (ShowEditDeadline)
            DeadlinePicker.InitializeFrom(Facade.State.Item.Activity?.DueAt);
    }

    private void CloseDeadlineForm() => ShowEditDeadline = false;

    [RelayCommand]
    private async Task UpsertDeadlineAsync()
    {
        var deadline = DeadlinePicker.SelectedDeadline;

        var response = await Facade.Gateway.UpsertDeadlineAsync(Facade.State.Item.Id, deadline);
        if (response.IsSuccess && Facade.State.Item.Activity is not null)
        {
            Facade.State.Item.Activity.DueAt = deadline;
            DeadlinePropertyChanged();
            CloseDeadlineForm();
        }
    }

    [RelayCommand]
    private async Task DeleteDeadlineAsync(CancellationToken ct)
    {
        DeadlinePicker.InitializeFrom(null);
        await UpsertDeadlineAsync();
    }
}
