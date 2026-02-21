using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    [ObservableProperty] private DateTimeOffset? _deadlineDate;

    [ObservableProperty] private TimeSpan? _deadlineTime;
    
    public DateTime? Deadline => Facade.State.Item?.Activity?.DueAt?.ToLocalTime().DateTime;
    public bool HasDeadline => Deadline is not null;
    public string DeadlineDisplay
        => Deadline is null
            ? string.Empty
            : Deadline.Value.ToString("ddd, dd MMM yyyy • HH:mm", CultureInfo.CurrentCulture);

    [RelayCommand]
    private async Task ShowUpsertDeadlinePanelAsync()
    {
    }

    [RelayCommand]
    private async Task DeleteDeadlineAsync(CancellationToken ct)
    {
    }
}