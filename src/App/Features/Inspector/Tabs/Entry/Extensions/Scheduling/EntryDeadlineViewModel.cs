using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Converters;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    [ObservableProperty] private DateTimeOffset? _deadlineDate;

    [ObservableProperty] private TimeSpan? _deadlineTime;
    
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
        
        if (Deadline is not null)
        {
            var localDateTime = Deadline.Value;
            DeadlineDate = new DateTimeOffset(DateTime.SpecifyKind(localDateTime.Date, DateTimeKind.Local));
            DeadlineTime = localDateTime.TimeOfDay;
            return;
        }
        
        DeadlineDate = null;
        DeadlineTime = null;
    }
    
    [RelayCommand]
    private async Task UpsertDeadlineAsync()
    {
        var deadline = DateTimeOffsetHelper.CombineLocalDateAndTimeToUtc(DeadlineDate, DeadlineTime);
        
        var response = await Facade.Gateway.UpsertDeadlineAsync(Facade.State.Item.Id, deadline);
        if (response.IsSuccess && Facade.State.Item.Activity is not null)
        {
            Facade.State.Item.Activity.DueAt = deadline;
            DeadlinePropertyChanged();
        }
    }

    [RelayCommand]
    private async Task DeleteDeadlineAsync(CancellationToken ct)
    {
        DeadlineDate = null;
        DeadlineTime = null;
        await UpsertDeadlineAsync();
    }
}