using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Presentation.Mapping;
using PageViewModel = Misa.Ui.Avalonia.Features.Tasks.Page.PageViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.Add;

public partial class AddTaskViewModel(PageViewModel vm) : ViewModelBase
{
    private PageViewModel MainViewModel { get; } = vm;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private Priority _selectedPriority;

    [ObservableProperty] private bool _titleHasValidationError;
    [ObservableProperty] private string _errorMessageTitle = string.Empty;

    public IReadOnlyList<Priority> Priorities { get; } = Enum.GetValues<Priority>();

    private void TitleValidationError(string message)
    {
        TitleHasValidationError = true;
        ErrorMessageTitle = message;
    }

    [RelayCommand]
    private async Task AddTask()
    {
        var trimmedTitle = Title.Trim();

        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            TitleValidationError("Please specify a title.");
            return;
        }

        var dto = new CreateItemDto
        {
            OwnerId = null,
            Priority = SelectedPriority,
            Title = trimmedTitle
        };

        var response = await MainViewModel.NavigationService.NavigationStore
            .MisaHttpClient.PostAsJsonAsync(requestUri: "api/tasks", dto);

        if (!response.IsSuccessStatusCode)
        { 
            var body = await response.Content.ReadAsStringAsync();
            var msg = string.IsNullOrWhiteSpace(body)
                ? $"Server returned {response.StatusCode}."
                : $"Server returned {response.StatusCode}: {body}";

            TitleValidationError(msg);
            return;
        }

        var createdItem = await response.Content.ReadFromJsonAsync<ReadItemDto>();
        if (createdItem == null)
        {
            TitleValidationError("Server returned success but no task payload.");
        }
    }
}
