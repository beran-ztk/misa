using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;

public sealed class CreateScheduleViewModel : IHostedForm<AddScheduleDto>
{
    public CreateScheduleViewModel(CreateScheduleState state)
    {
        State = state;
    }

    public CreateScheduleState State { get; }

    public string Title => "Create new Schedule";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    public AddScheduleDto? TrySubmit()
        => State.TryGetValidatedRequestObject();
}