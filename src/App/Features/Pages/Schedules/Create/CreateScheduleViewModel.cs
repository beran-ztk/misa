using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schedules;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;
using CreateScheduleState = Misa.Ui.Avalonia.Features.Pages.Schedules.Root.CreateScheduleState;

namespace Misa.Ui.Avalonia.Features.Pages.Schedules.Create;

public sealed class CreateScheduleViewModel(
    CreateScheduleState state,
    ScheduleGateway gateway) : IHostedForm<ScheduleDto>
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
