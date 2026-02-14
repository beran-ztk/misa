using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class CreateTaskState : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private TaskCategoryDto _selectedCategoryDto;
    [ObservableProperty] private PriorityDto _selectedPriorityDto;

    [ObservableProperty] private bool _titleHasValidationError;
    [ObservableProperty] private string _errorMessageTitle = string.Empty;

    public IReadOnlyList<TaskCategoryDto> Categories { get; } = Enum.GetValues<TaskCategoryDto>();
    public IReadOnlyList<PriorityDto> Priorities { get; } = Enum.GetValues<PriorityDto>();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasSubmitError;
    [ObservableProperty] private string _submitErrorMessage = string.Empty;
    public void ClearSubmitError()
    {
        HasSubmitError = false;
        SubmitErrorMessage = string.Empty;
    }
    public void SetSubmitError(string message)
    {
        HasSubmitError = true;
        SubmitErrorMessage = message;
    }
    private void TitleValidationError(string message)
    {
        TitleHasValidationError = true;
        ErrorMessageTitle = message;
    }
    public CreateTaskDto? TryGetValidatedRequestObject()
    {
        TitleHasValidationError = false;
        ErrorMessageTitle = string.Empty;

        if (!string.IsNullOrWhiteSpace(Title))
            return new CreateTaskDto(Title, SelectedCategoryDto, SelectedPriorityDto);

        TitleValidationError("Please specify a title.");
        return null;
    }
}