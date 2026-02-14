using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;

public sealed class CreateScheduleViewModel(
    CreateScheduleState state,
    SchedulerGateway gateway) : IHostedForm<ScheduleDto>
{
    public CreateScheduleState State { get; } = state;

    public string Title => "Create new Schedule";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    public async Task<Result<ScheduleDto>> SubmitAsync()
    {
        var dto = State.TryGetValidatedRequestObject();
        if (dto is null)
            return Result<ScheduleDto>.Failure(message: "Validation failed.");

        return await gateway.CreateAsync(dto);
    }
}
