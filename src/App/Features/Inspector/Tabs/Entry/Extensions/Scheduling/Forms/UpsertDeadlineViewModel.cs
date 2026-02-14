using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Common.Deadlines;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Scheduling.Forms;

public sealed partial class UpsertDeadlineViewModel(
    Guid itemId,
    DateTime? existingDeadlineLocal,
    InspectorGateway gateway)
    : ViewModelBase, IHostedForm<DeadlineDto>
{
    // Host
    public string Title => existingDeadlineLocal is null ? "Create deadline" : "Edit deadline";
    public string SubmitText => "Save";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    [ObservableProperty]
    private DateTimeOffset? _deadlineDate;

    [ObservableProperty]
    private TimeSpan? _deadlineTime;

    public async Task<Result<DeadlineDto>> SubmitAsync()
    {
        if (DeadlineDate is null || DeadlineTime is null)
            return Result<DeadlineDto>.Failure("Validation failed.");

        var localDate = DeadlineDate.Value.Date;
        var localDateTime = localDate + DeadlineTime.Value;

        var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Local);
        var dueAtUtc = new DateTimeOffset(local).ToUniversalTime();

        var dto = new UpsertDeadlineDto(itemId, dueAtUtc);

        return await gateway.UpsertDeadlineAsync(dto);
    }
}