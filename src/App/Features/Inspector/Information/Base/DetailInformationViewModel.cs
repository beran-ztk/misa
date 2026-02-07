using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Common.Deadline;
using Misa.Ui.Avalonia.Features.Inspector.Common;
using ReactiveUI;
using DescriptionViewModel = Misa.Ui.Avalonia.Features.Inspector.Information.Extensions.Descriptions.DescriptionViewModel;
using DetailMainWindowViewModel = Misa.Ui.Avalonia.Features.Inspector.Main.DetailMainWindowViewModel;
using SessionViewModel = Misa.Ui.Avalonia.Features.Inspector.Information.Extensions.Sessions.SessionViewModel;

namespace Misa.Ui.Avalonia.Features.Inspector.Information.Base;

public partial class DetailInformationViewModel : ViewModelBase
{
    public DetailMainWindowViewModel Parent { get; }
    public DescriptionViewModel Description { get; }
    public SessionViewModel Session { get; }
    public DeadlineSectionViewModel DeadlineSection { get; }
    public DetailInformationViewModel(DetailMainWindowViewModel parent)
    {
        Parent = parent;
        Description = new DescriptionViewModel(this);
        Session = new SessionViewModel(this);
        DeadlineSection = new DeadlineSectionViewModel(this);
        
        Parent.WhenAnyValue(x => x.Extension)
            .Subscribe(_ =>
            {
                OnPropertyChanged(nameof(TaskExtension));
                OnPropertyChanged(nameof(HasTaskExtension));
                OnPropertyChanged(nameof(HasDeadline));
            });
    }
    public TaskExtensionVm? TaskExtension => Parent.Extension as TaskExtensionVm;
    public bool HasTaskExtension => TaskExtension is not null;
    public bool HasDeadline => Parent.Deadline.DueAtUtc is not null;

    [RelayCommand]
    private void CopyId()
    {
        // Parent.EntityDetailHost.NavigationService.ClipboardService.SetTextAsync(Parent.Item.Id.ToString());
    }
    
    // Edit State
    [ObservableProperty] private bool _isEditStateOpen;
    [ObservableProperty] private int _stateId;
    [ObservableProperty] private List<StateDto> _settableStates = []; 

    [RelayCommand]
    private async Task EditState()
    {
        await SetUserSettableStates();
        if (SettableStates.Count < 1)
            return;

        StateId = Enumerable.First<StateDto>(SettableStates).Id;
        IsEditStateOpen = true;
    }

    [RelayCommand]
    private void CloseEditState()
    {
        StateId = 0;
        SettableStates.Clear();
        IsEditStateOpen = false;
    }

    [RelayCommand]
    private async Task SaveEditState()
    {
        // var currentId = Parent.Item.StateId;
        // var selectedId = StateId;
        //
        // if (currentId == selectedId)
        // {
        //     CloseEditState();
        //     return;
        // }
        //
        // var currentEntityId = Parent.Item.Id;
        //
        // UpdateItemDto itemDto = new()
        // {
        //     EntityId = currentEntityId,
        //     StateId = selectedId
        // };
        // var response = await Parent.EntityDetailHost.NavigationService.NavigationStore
        //     .MisaHttpClient.PatchAsJsonAsync(requestUri: "tasks", itemDto);
        //
        // if (!response.IsSuccessStatusCode)
        //     Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");
        //
        // await Parent.Reload();
        // CloseEditState();
    }
    
    private async Task SetUserSettableStates()
    {
        // try
        // {
        //     var states = await Parent.EntityDetailHost.NavigationService.NavigationStore.MisaHttpClient
        //         .GetFromJsonAsync<List<StateDto>>(requestUri: $"Lookups/UserSettableStates?stateId={Parent.Item.Id}");
        //
        //     if (states == null)
        //         return;
        //     
        //     SettableStates = states;
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e);
        // }
    }
    
    [ObservableProperty] private bool _isEditTitleFormOpen;
    [ObservableProperty] private string _title = string.Empty;
    [RelayCommand]
    private void ShowEditTitleForm()
    {
        Title = Parent.Item.Title;
        IsEditTitleFormOpen = true;
    }
    [RelayCommand]
    private void CloseEditTitleForm() => IsEditTitleFormOpen = false;

    [RelayCommand]
    private async Task UpdateTitleTask()
    {
        // var dto = new UpdateItemDto
        // {
        //     EntityId = Parent.Item.Id,
        //     Title = Parent.Item.Title
        // };
        //
        // var response = await Parent.EntityDetailHost.NavigationService.NavigationStore
        //     .MisaHttpClient.PatchAsJsonAsync(requestUri: "tasks", dto);
        //
        // if (!response.IsSuccessStatusCode)
        //     Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");
        //
        // await Parent.Reload();
        // IsEditTitleFormOpen = false;
    }
    
    [RelayCommand]
    private async Task DeleteEntity()
    {
        // try
        // {
        //     var id = Parent.Item.Id;
        //     await Parent.EntityDetailHost.NavigationService.NavigationStore
        //         .MisaHttpClient.PatchAsync(requestUri: $"Entity/Delete?entityId={id}", content: null);
        //     
        //     await Parent.Reload();
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e);
        // }
    }
    [RelayCommand]
    private async Task ArchiveEntity()
    {
        // try
        // {
        //     var id = Parent.Item.Id;
        //     await Parent.EntityDetailHost.NavigationService.NavigationStore
        //         .MisaHttpClient.PatchAsync(requestUri: $"Entity/Archive?entityId={id}", content: null);
        //     
        //     await Parent.Reload();
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e);
        // }
    }
}