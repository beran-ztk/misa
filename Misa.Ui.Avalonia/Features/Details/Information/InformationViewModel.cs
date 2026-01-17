using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Features.Details.Information.Extensions.Sessions;
using Misa.Ui.Avalonia.Features.Details.Page;
using Misa.Ui.Avalonia.Presentation.Mapping;
using DescriptionViewModel = Misa.Ui.Avalonia.Features.Details.Information.Extensions.Descriptions.DescriptionViewModel;

namespace Misa.Ui.Avalonia.Features.Details.Information;

public partial class InformationViewModel : ViewModelBase
{
    public DetailPageViewModel Parent { get; }
    public DescriptionViewModel Description { get; }
    public SessionViewModel Session { get; }

    public InformationViewModel(DetailPageViewModel parent)
    {
        Parent = parent;
        Description = new DescriptionViewModel(this);
        Session = new SessionViewModel(this);
    }

    [RelayCommand]
    private void CopyId()
    {
        Parent.EntityDetailHost.NavigationService.ClipboardService.SetTextAsync(Parent.ItemOverview.Item.Id.ToString());
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

        StateId = SettableStates.First().Id;
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
        var currentId = Parent.ItemOverview.Item.State.Id;
        var selectedId = StateId;

        if (currentId == selectedId)
        {
            CloseEditState();
            return;
        }

        var currentEntityId = Parent.ItemOverview.Item.Id;

        UpdateItemDto itemDto = new()
        {
            EntityId = currentEntityId,
            StateId = selectedId
        };
        var response = await Parent.EntityDetailHost.NavigationService.NavigationStore
            .MisaHttpClient.PatchAsJsonAsync(requestUri: "tasks", itemDto);
        
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

        await Parent.Reload();
        CloseEditState();
    }
    
    private async Task SetUserSettableStates()
    {
        try
        {
            var states = await Parent.EntityDetailHost.NavigationService.NavigationStore
                .MisaHttpClient.GetFromJsonAsync<List<StateDto>>(requestUri: $"Lookups/UserSettableStates?stateId={Parent.ItemOverview.Item.Id}");

            if (states == null)
                return;
            
            SettableStates = states;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    [ObservableProperty] private bool _isEditTitleFormOpen;
    [ObservableProperty] private string _title = string.Empty;
    [RelayCommand]
    private void ShowEditTitleForm()
    {
        Title = Parent.ItemOverview.Item.Title;
        IsEditTitleFormOpen = true;
    }
    [RelayCommand]
    private void CloseEditTitleForm() => IsEditTitleFormOpen = false;

    [RelayCommand]
    private async Task UpdateTitleTask()
    {
        var dto = new UpdateItemDto
        {
            EntityId = Parent.ItemOverview.Item.Id,
            Title = Title == Parent.ItemOverview.Item.Title
                ? null 
                : Title
        };

        var response = await Parent.EntityDetailHost.NavigationService.NavigationStore
            .MisaHttpClient.PatchAsJsonAsync(requestUri: "tasks", dto);

        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

        await Parent.Reload();
        IsEditTitleFormOpen = false;
    }

    

    
 


    

    

    [RelayCommand]
    private async Task DeleteEntity()
    {
        try
        {
            var id = Parent.ItemOverview.Item.Id;
            await Parent.EntityDetailHost.NavigationService.NavigationStore
                .MisaHttpClient.PatchAsync(requestUri: $"Entity/Delete?entityId={id}", content: null);
            
            await Parent.Reload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    [RelayCommand]
    private async Task ArchiveEntity()
    {
        try
        {
            var id = Parent.ItemOverview.Item.Id;
            await Parent.EntityDetailHost.NavigationService.NavigationStore
                .MisaHttpClient.PatchAsync(requestUri: $"Entity/Archive?entityId={id}", content: null);
            
            await Parent.Reload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    // Deadline
    [ObservableProperty] private bool _isDeadlineFormOpen;
    [ObservableProperty] private DateTimeOffset? _deadlineDate;
    [ObservableProperty] private TimeSpan? _deadlineTime;

    // [RelayCommand]
    // private void ShowDeadlineForm()
    // {
    //     IsDeadlineFormOpen = true;
    //
    //     var existingUtc = parent.DetailedEntity?.Item?.ScheduledDeadline?.DeadlineAt;
    //     var local = existingUtc?.ToLocalTime() ?? DateTimeOffset.Now;
    //
    //     DeadlineDate = local.Date;
    //     DeadlineTime = local.TimeOfDay;
    // }
    //
    // [RelayCommand]
    // private void CloseDeadlineForm()
    // {
    //     IsDeadlineFormOpen = false;
    // }
    //
    // [RelayCommand]
    // private async Task UpsertDeadline()
    // {
    //     try
    //     {
    //         if (parent.DetailedEntity?.Item is null || DeadlineDate is null || DeadlineTime is null)
    //             return;
    //
    //         var localDateTime = DeadlineDate.Value.Date + DeadlineTime.Value;
    //         var localOffset = TimeZoneInfo.Local.GetUtcOffset((DateTime)localDateTime);
    //         var deadlineAt = new DateTimeOffset(localDateTime, localOffset);
    //
    //         var dto = new ScheduleDeadlineDto(DeadlineAt: deadlineAt);
    //
    //         var response = await parent.EntityDetailHost.NavigationService.NavigationStore
    //             .MisaHttpClient.PutAsJsonAsync(
    //                 requestUri: $"items/{parent.DetailedEntity.Item.EntityId}/deadline",
    //                 dto
    //             );
    //
    //         if (!response.IsSuccessStatusCode)
    //             Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");
    //
    //         await parent.Reload();
    //         CloseDeadlineForm();
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine(e);
    //     }
    // }
    //
    // // Remove Deadline
    // private CancellationTokenSource? _removeDeadlineCts;
    //
    // [ObservableProperty]
    // [NotifyCanExecuteChangedFor(nameof(RemoveDeadlineCommand))]
    // [NotifyCanExecuteChangedFor(nameof(CancelRemoveDeadlineCommand))]
    // private bool _isRemovingDeadline;
    // private bool CanCancelRemoveDeadline() => IsRemovingDeadline;
    //
    // [RelayCommand(CanExecute = nameof(CanCancelRemoveDeadline))]
    // private void CancelRemoveDeadline()
    // {
    //     _removeDeadlineCts?.Cancel();
    // }
    //
    // [RelayCommand]
    // private async Task RemoveDeadline()
    // {
    //     if (parent.DetailedEntity?.Item is null)
    //     {
    //         return;
    //     }
    //     
    //     _removeDeadlineCts?.Dispose();
    //     _removeDeadlineCts = new CancellationTokenSource();
    //
    //     IsRemovingDeadline = true;
    //     
    //     try
    //     {
    //         var itemId = parent.DetailedEntity.Item.EntityId;
    //
    //         using var request = new HttpRequestMessage(HttpMethod.Delete, $"items/{itemId}/deadline");
    //
    //         var response = await parent.EntityDetailHost.NavigationService.NavigationStore
    //             .MisaHttpClient.SendAsync(request, _removeDeadlineCts.Token);
    //
    //         if (!response.IsSuccessStatusCode)
    //         {
    //             Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");
    //         }
    //     }
    //     catch (OperationCanceledException)
    //     {
    //         Console.WriteLine("RemoveDeadline canceled by user.");
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine(e);
    //     }
    //     finally
    //     {
    //         _removeDeadlineCts?.Dispose();
    //         _removeDeadlineCts = null;
    //         
    //         IsRemovingDeadline = false;
    //     }
    // }
}