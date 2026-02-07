using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Common.Deadline;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Add;

public partial class AddTaskViewModel : ViewModelBase
{
    public DeadlineInputViewModel Deadline { get; } = new();

    [ObservableProperty] private bool _createMore;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private TaskCategoryContract _selectedCategoryContract;
    [ObservableProperty] private PriorityContract _selectedPriorityContract;

    [ObservableProperty] private bool _titleHasValidationError;
    [ObservableProperty] private string _errorMessageTitle = string.Empty;

    public IReadOnlyList<TaskCategoryContract> Categories { get; } = Enum.GetValues<TaskCategoryContract>();
    public IReadOnlyList<PriorityContract> Priorities { get; } = Enum.GetValues<PriorityContract>();

    private void TitleValidationError(string message)
    {
        TitleHasValidationError = true;
        ErrorMessageTitle = message;
    }

    public event Action<AddTaskDto>? Completed;
    public event Action? Cancelled;
    [RelayCommand]
    private void Confirm()
    {
        var trimmed = Title.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            TitleValidationError("Please specify a title.");
            return;
        }

        var deadlineDto = Deadline.ToDtoOrNull();
        var dto = new AddTaskDto(
            trimmed,
            SelectedCategoryContract,
            SelectedPriorityContract,
            deadlineDto
        );

        Completed?.Invoke(dto);
    }
    [RelayCommand]
    private void Cancel()
        => Cancelled?.Invoke();
}
