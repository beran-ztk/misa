using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Presentation.Mapping;
using PageViewModel = Misa.Ui.Avalonia.Features.Tasks.Page.PageViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.Add;

public partial class AddTaskViewModel(PageViewModel vm) : ViewModelBase
{
    private PageViewModel Parent { get; } = vm;

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

    [RelayCommand]
    private async Task AddTask()
    {
        try
        {
            var trimmedTitle = Title.Trim();

            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                TitleValidationError("Please specify a title.");
                return;
            }
            
            var addTaskDto = new AddTaskDto(Title, SelectedCategoryContract, SelectedPriorityContract);

            using var request = new HttpRequestMessage(HttpMethod.Post, "tasks");
            request.Content = JsonContent.Create(addTaskDto);
            
            using var response = await Parent.NavigationService.NavigationStore.MisaHttpClient
                .SendAsync(request, CancellationToken.None);

            response.EnsureSuccessStatusCode();

            var createdTask = await response.Content.ReadFromJsonAsync<Result<TaskDto>>(CancellationToken.None);
            if (createdTask?.Value != null)
            {
                await Parent.AddToCollection(createdTask.Value);
                if (CreateMore)
                {
                    Title = string.Empty;
                }
                else
                {
                    Close(createdTask.Value);   
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    [RelayCommand]
    private void Close(TaskDto? dto = null)
    {
        Title = string.Empty;
        Parent.InfoView = null;

        if (dto != null)
        {
            Parent.SelectedTask = dto;
        }
    }
}
