using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;
using Misa.Ui.Avalonia.Common.Converters;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class CreateTaskState : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private TaskCategoryDto _selectedCategoryDto;
    [ObservableProperty] private ActivityPriorityDto _selectedActivityPriorityDto;
    
    [ObservableProperty] private DateTimeOffset? _deadlineDate;
    [ObservableProperty] private TimeSpan? _deadlineTime;

    [ObservableProperty] private bool _titleHasValidationError;
    [ObservableProperty] private string _errorMessageTitle = string.Empty;

    public IReadOnlyList<TaskCategoryDto> Categories { get; } = Enum.GetValues<TaskCategoryDto>();
    public IReadOnlyList<ActivityPriorityDto> Priorities { get; } = Enum.GetValues<ActivityPriorityDto>();

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

        if (string.IsNullOrWhiteSpace(Title))
            TitleValidationError("Please specify a title.");

        var dueAtUtc = DateTimeOffsetHelper.CombineLocalDateAndTimeToUtc(DeadlineDate, DeadlineTime);
            
        return new CreateTaskDto(Title, Description, SelectedCategoryDto, SelectedActivityPriorityDto, dueAtUtc);
    }
}