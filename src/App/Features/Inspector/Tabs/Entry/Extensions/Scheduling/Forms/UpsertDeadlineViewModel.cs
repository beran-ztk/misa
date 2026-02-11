using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Scheduling.Forms;

public partial class UpsertDeadlineViewModel(Guid itemId, DateTime? existingDeadlineLocal) : ViewModelBase, IHostedForm<UpsertDeadlineDto>
{
    // Host
    public string Title => existingDeadlineLocal is null ? "Create deadline" : "Edit deadline";
    public string SubmitText => "Save";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    public UpsertDeadlineDto? TrySubmit() => Submit();

    // Content (local input)
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private DateTimeOffset? _deadlineDate;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private TimeSpan? _deadlineTime;

    private UpsertDeadlineDto? Submit()
    {
        if (DeadlineDate is null || DeadlineTime is null)
            return null;

        var localDate = DeadlineDate.Value.Date;
        var localDateTime = localDate + DeadlineTime.Value;

        var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Local);
        var dueAtUtc = new DateTimeOffset(local).ToUniversalTime();

        return new UpsertDeadlineDto(itemId, dueAtUtc);
    }
}
