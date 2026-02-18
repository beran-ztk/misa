using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Chronicle;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Pages.Chronicle.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle.Create;

// Du ersetzt TResult später durch dein JournalEntryDto (oder was du im UI willst)
public sealed partial class CreateJournalViewModel(
    CreateJournalState state,
    ChronicleGateway gateway)
    : ObservableObject, IHostedForm<JournalEntryDto>
{
    public CreateJournalState State { get; } = state;

    public string Title => "Create journal entry";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";

    public bool CanSubmit => !State.IsBusy;

    public async Task<Result<JournalEntryDto>> SubmitAsync()
    {
        State.ClearSubmitError();

        var dto = State.TryGetValidatedRequestObject();
        if (dto is null)
            return Result<JournalEntryDto>.Failure(message: "Validation failed.");

        State.IsBusy = true;
        try
        {
            // Placeholder: ersetze mit gateway.CreateEntryAsync(CreateJournalEntryDto)
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