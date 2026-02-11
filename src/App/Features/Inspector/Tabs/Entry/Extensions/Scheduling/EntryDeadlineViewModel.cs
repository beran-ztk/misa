using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDeadline))]
    private DateTime? _deadline;

    public bool HasDeadline => Deadline is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateDeadline))]
    private DateTimeOffset? _deadlineDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateDeadline))]
    private TimeSpan? _deadlineTime;
    public bool CanCreateDeadline => DeadlineDate is not null && DeadlineTime is not null;

    private void ResetDeadlineDraft()
    {
        DeadlineDate = null;
        DeadlineTime = null;
    }

    [RelayCommand]
    private async Task CreateDeadlineAsync(CancellationToken ct)
    {
        if (DeadlineDate is null || DeadlineTime is null)
            return;

        var localDate = DeadlineDate.Value.Date;
        var localDateTime = localDate + DeadlineTime.Value;

        var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Local);
        var dueAtUtc = new DateTimeOffset(local).ToUniversalTime();

        await Facade.Gateway.CreateDeadlineAsync(Facade.State.Item.Id, dueAtUtc, ct);

        await Facade.Reload();
        ResetDeadlineDraft();
    }

    [RelayCommand]
    private void CancelDeadlineDraft()
    {
        ResetDeadlineDraft();
    }

    [RelayCommand]
    private async Task DeleteDeadlineAsync(CancellationToken ct)
    {
        await Facade.Gateway.DeleteDeadlineAsync(Facade.State.Item.Id, ct);

        await Facade.Reload();
        ResetDeadlineDraft();
    }
}