using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;
using Misa.Ui.Avalonia.Common.Components.DeadlinePicker;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks;

public sealed partial class CreateTaskViewModel(TaskGateway gateway)
    : ViewModelBase, IHostedForm<TaskDto>
{
    public string FormTitle { get; } = "Create Task";
    public string? FormDescription { get; }

    public async Task<Result<TaskDto>> SubmitAsync()
    {
        var request = new CreateTaskRequest(
            Title,
            Description,
            SelectedCategoryDto,
            SelectedActivityPriorityDto,
            DeadlinePicker.SelectedDeadline);

        return await gateway.CreateAsync(request);
    }

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private TaskCategoryDto _selectedCategoryDto;
    [ObservableProperty] private ActivityPriorityDto _selectedActivityPriorityDto;

    public DeadlinePickerViewModel DeadlinePicker { get; } = new();

    public IReadOnlyList<TaskCategoryDto> Categories { get; } = Enum.GetValues<TaskCategoryDto>();
    public IReadOnlyList<ActivityPriorityDto> Priorities { get; } = Enum.GetValues<ActivityPriorityDto>();
}