using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Create;

public sealed partial class CreateTaskViewModel(CreateTaskState state) : ObservableObject, IHostedForm<CreateTaskDto>
{
    public CreateTaskState State { get; } = state;

    public string Title => "Create new Task";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";

    public bool CanSubmit => true;

    public CreateTaskDto? TrySubmit() => State.TryGetValidatedRequestObject();
}