using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Chronicle;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle.Root;

public sealed partial class CreateJournalState : ObservableObject
{
    [ObservableProperty] private string _description = string.Empty;

    // Occurred
    [ObservableProperty] private DateTimeOffset _occurredDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private TimeSpan? _occurredTime = DateTimeOffset.Now.TimeOfDay;

    // Until (optional)
    [ObservableProperty] private DateTimeOffset? _untilDate;
    [ObservableProperty] private TimeSpan? _untilTime;

    // Category (du ersetzt Typ/Source später)
    [ObservableProperty] private object? _selectedCategory;
    public IReadOnlyList<object> Categories { get; } = [];

    // Validation
    [ObservableProperty] private bool _descriptionHasValidationError;
    [ObservableProperty] private string _errorMessageDescription = string.Empty;

    [ObservableProperty] private bool _occurredHasValidationError;
    [ObservableProperty] private string _errorMessageOccurred = string.Empty;

    [ObservableProperty] private bool _untilHasValidationError;
    [ObservableProperty] private string _errorMessageUntil = string.Empty;

    [ObservableProperty] private bool _categoryHasValidationError;
    [ObservableProperty] private string _errorMessageCategory = string.Empty;

    // Submit
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

    public void Reset()
    {
        Description = string.Empty;

        OccurredDate = DateTimeOffset.Now.Date;
        OccurredTime = DateTimeOffset.Now.TimeOfDay;

        UntilDate = null;
        UntilTime = null;

        SelectedCategory = null;

        ClearValidation();
        ClearSubmitError();
        IsBusy = false;
    }

    private void ClearValidation()
    {
        DescriptionHasValidationError = false;
        ErrorMessageDescription = string.Empty;

        OccurredHasValidationError = false;
        ErrorMessageOccurred = string.Empty;

        UntilHasValidationError = false;
        ErrorMessageUntil = string.Empty;

        CategoryHasValidationError = false;
        ErrorMessageCategory = string.Empty;
    }

    public CreateJournalEntryDto? TryGetValidatedRequestObject()
    {
        ClearValidation();

        var trimmed = Description.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            DescriptionHasValidationError = true;
            ErrorMessageDescription = "Please specify a description.";
            return null;
        }

        if (OccurredTime is null)
        {
            OccurredHasValidationError = true;
            ErrorMessageOccurred = "Please specify a time.";
            return null;
        }

        var occurredAt = OccurredDate.Date.Add(OccurredTime.Value);

        DateTimeOffset? untilAt = null;
        if (UntilDate is not null)
        {
            var time = UntilTime ?? TimeSpan.Zero;
            untilAt = UntilDate.Value.Date.Add(time);

            if (untilAt.Value < occurredAt)
            {
                UntilHasValidationError = true;
                ErrorMessageUntil = "Until must be >= occurred.";
                return null;
            }
        }
        else if (UntilTime is not null)
        {
            UntilHasValidationError = true;
            ErrorMessageUntil = "Please select a date if you specify an until time.";
            return null;
        }

        // Category: optional (wenn du required willst, schalte den Block ein)
        Guid? categoryId = null;
        if (SelectedCategory is Guid g)
            categoryId = g;

        return new CreateJournalEntryDto(
            Description: trimmed,
            OccurredAt: occurredAt,
            UntilAt: untilAt,
            CategoryId: categoryId
        );
    }
}
