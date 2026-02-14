using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Create;

public sealed partial class CreateTaskViewModel(
    CreateTaskState state,
    TaskGateway gateway) : ObservableObject, IHostedCommitForm<TaskDto>
{
    public CreateTaskState State { get; } = state;

    public string Title => "Create new Task";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";

    public bool CanSubmit => !State.IsBusy;

    public async Task<Result<TaskDto>> SubmitAsync()
    {
        State.ClearSubmitError();

        var dto = State.TryGetValidatedRequestObject();
        if (dto is null)
            return Result<TaskDto>.Failure(message: "Validation failed.");

        State.IsBusy = true;
        try
        {
            var result = await gateway.CreateAsync(dto);
            if (result is { IsSuccess: false, Error: not null })
                State.SetSubmitError(result.Error.Message);
            
            return result;
        }
        finally
        {
            State.IsBusy = false;
        }
    }
}
