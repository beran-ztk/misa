using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Scheduler.Add;

public partial class AddScheduleViewModel(INavigationService navigationService) : ViewModelBase
{
    private INavigationService NavigationService { get; } = navigationService;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private ScheduleFrequencyTypeContract _selectedFrequencyType = ScheduleFrequencyTypeContract.Minutes;
    [ObservableProperty] private int _frequencyInterval = 1;

    [ObservableProperty] private bool _hasValidationError;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public IReadOnlyList<ScheduleFrequencyTypeContract> FrequencyTypes { get; } = Enum.GetValues<ScheduleFrequencyTypeContract>();

    private void ShowValidationError(string message)
    {
        HasValidationError = true;
        ErrorMessage = message;
    }

    private void ClearValidationError()
    {
        HasValidationError = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveSchedule()
    {
        try
        {
            ClearValidationError();

            if (FrequencyInterval <= 0)
            {
                ShowValidationError("Frequency interval must be greater than 0");
                return;
            }

            var addScheduleDto = new AddScheduleDto
            {
                Title = Title,
                ScheduleFrequencyType = SelectedFrequencyType,
                FrequencyInterval = FrequencyInterval
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "scheduling");
            request.Content = JsonContent.Create(addScheduleDto);

            using var response = await NavigationService.NavigationStore.MisaHttpClient
                .SendAsync(request, CancellationToken.None);
            
            var result = await response.Content.ReadFromJsonAsync<Result>(CancellationToken.None);
            
            if (result?.IsSuccess == true)
            {
                Close();
            }
            else
            {
                ShowValidationError(result?.Error?.Message ?? "Failed to create schedule");
            }
        }
        catch (Exception e)
        {
            ShowValidationError($"Error: {e.Message}");
        }
    }

    [RelayCommand]
    private void Close()
    {
        Title = string.Empty;
        SelectedFrequencyType = ScheduleFrequencyTypeContract.Minutes;
        FrequencyInterval = 1;
        ClearValidationError();
    }
}