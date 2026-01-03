using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Audit;
using Misa.Contract.Audit.Lookups;
using Misa.Contract.Events;
using Misa.Contract.Items;
using Misa.Contract.Items.Lookups;
using Misa.Contract.Main;
using Misa.Contract.Scheduling;
using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Details;

public partial class DetailInformationViewModel : ViewModelBase, IDisposable
{
    public IEntityDetail EntityDetail { get; }
    public DetailMainDetailViewModel Parent { get; }
    private string _description = string.Empty;
    public ReactiveCommand<Unit, Unit> AddDescriptionCommand { get; }
    public ReactiveCommand<Unit, Unit> StartSessionCommand { get; }
    public ReactiveCommand<Unit, Unit> PauseSessionCommand { get; }
    private readonly NavigationStore _nav;
    private bool _disposed;

    public DetailInformationViewModel(DetailMainDetailViewModel parent, bool listenRealtime = false)
    {
        Parent = parent;
        EntityDetail = parent.EntityDetail;
        
        _nav = parent.NavigationService.NavigationStore;
        
        if (listenRealtime)
        {
            Console.WriteLine($"[VM] subscribe nav={_nav.GetHashCode()}");
            _nav.RealtimeEventReceived += OnRealtimeEvent;
        }
        
        AddDescriptionCommand = ReactiveCommand.CreateFromTask(AddDescriptionAsync);
        AddDescriptionCommand.Subscribe();
        
        StartSessionCommand = ReactiveCommand.CreateFromTask(StartSessionAsync);
        StartSessionCommand.Subscribe();
        
        PauseSessionCommand = ReactiveCommand.CreateFromTask(EndSessionAsync);
        PauseSessionCommand.Subscribe();
        
        this.WhenAnyValue(x => x.EntityDetail.SelectedEntity)
            .Subscribe(_ =>
            {
                ResetDescription();
            });
    }
    private void OnRealtimeEvent(EventDto evt)
    {
        Console.WriteLine($"[VM] event {evt.EventType}");

        if (!string.Equals(evt.EventType, "DeadlineRemoved", StringComparison.Ordinal))
            return;

        Guid itemId;
        try
        {
            using var doc = JsonDocument.Parse(evt.Payload);
            itemId = doc.RootElement.GetProperty("itemId").GetGuid();
            Console.WriteLine($"[Information] item={itemId}, store={GetHashCode()}");
        }
        catch { return; }

        var currentItemId = Parent.DetailedEntity?.Item?.EntityId;
        if (currentItemId is null || currentItemId.Value != itemId)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            Console.WriteLine($"[InformationView] refresh Ui, store={GetHashCode()}");
            Parent.Refresh();
            CloseDeadlineForm(); // optional
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _nav.RealtimeEventReceived -= OnRealtimeEvent;
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
        var currentId = Parent.DetailedEntity?.Item?.State.Id;
        var selectedId = StateId;

        if (currentId == null)
            return;
        if (currentId == selectedId)
        {
            CloseEditState();
            return;
        }

        var currentEntityId = Parent.DetailedEntity?.Id;
        if (currentEntityId == null)
            return;

        UpdateItemDto itemDto = new()
        {
            EntityId = (Guid)currentEntityId,
            StateId = selectedId
        };
        var response = await Parent.NavigationService.NavigationStore
            .MisaHttpClient.PatchAsJsonAsync(requestUri: "tasks", itemDto);
        
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

        Parent.Refresh(true);
        CloseEditState();
    }
    
    private async Task SetUserSettableStates()
    {
        try
        {
            var currentStateId = Parent.DetailedEntity?.Item?.State.Id;
            if (currentStateId == null)
                return;
            
            var states = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.GetFromJsonAsync<List<StateDto>>(requestUri: $"Lookups/UserSettableStates?stateId={currentStateId}");

            if (states == null)
                return;
            
            SettableStates = states;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    // Session
    [ObservableProperty] private bool isStartFormOpen;
    [ObservableProperty] private bool isPauseFormOpen;
    [ObservableProperty] private bool _isStopFormOpen;
    
    [ObservableProperty] private int? plannedMinutes;
    [ObservableProperty] private string? objective;
    [ObservableProperty] private bool? stopAutomatically;
    [ObservableProperty] private string? autoStopReason;
    
    [ObservableProperty] private string? _pauseReason;
    
    [ObservableProperty] private int? efficiency;
    [ObservableProperty] private int? concentration;
    [ObservableProperty] private string? summary;
    

    [ObservableProperty] private bool isEditTitleFormOpen;
    [ObservableProperty] private string title;
    [RelayCommand]
    private void ShowEditTitleForm()
    {
        if (Parent.DetailedEntity?.Item is null) return;

        Title = Parent.DetailedEntity.Item.Title;
        IsEditTitleFormOpen = true;
    }
    [RelayCommand]
    private void CloseEditTitleForm() => IsEditTitleFormOpen = false;

    [RelayCommand]
    private async Task UpdateTitleTask()
    {
        if (Parent.DetailedEntity == null)
            return;
        
        var dto = new UpdateItemDto
        {
            EntityId = Parent.DetailedEntity.Id,
            Title = Title == Parent.DetailedEntity?.Item?.Title
                ? null 
                : Title
            // StateId = StateId == Parent.DetailedEntity?.Item?.State.Id 
            //     ? null 
            //     : StateId
        };

        var response = await Parent.NavigationService.NavigationStore
            .MisaHttpClient.PatchAsJsonAsync(requestUri: "tasks", dto);

        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

        Parent.Refresh(true);
        IsEditTitleFormOpen = false;
    }

    [RelayCommand]
    private void ShowSessionStartForm()
    {
        IsStartFormOpen = true;
        PlannedMinutes = null;
        Objective = null;
        StopAutomatically = false;
        AutoStopReason = null;
    }

    [RelayCommand]
    private void CloseSessionStartForm()
    {
        IsStartFormOpen = false;
    }

    [RelayCommand]
    private void ShowSessionPauseForm()
    {
        IsPauseFormOpen = !IsPauseFormOpen;
    } 

    [RelayCommand]
    private void CloseSessionPauseForm()
    {
        IsPauseFormOpen = false;
    } 
    
    [RelayCommand]
    private void ShowSessionStopForm()
    {
        Summary = null;
        Concentration = null;
        EfficiencyId = null;
        ConcentrationId = null;
        IsStopFormOpen = true;
    }

    [RelayCommand]
    private void CloseSessionStopForm()
    {
        IsStopFormOpen = false;
    }
    
    [ObservableProperty] private int? efficiencyId; 
    [ObservableProperty] private int? concentrationId; 
    public IReadOnlyList<SessionEfficiencyTypeDto> EfficiencyTypes =>
        Parent.NavigationService.LookupsStore.EfficiencyTypes;
    public IReadOnlyList<SessionConcentrationTypeDto> ConcentrationTypes =>
        Parent.NavigationService.LookupsStore.ConcentrationTypes;
    
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
    public void ResetDescription()
        => Description = string.Empty;

    [RelayCommand]
    private async Task SessionContinue()
    {
        try
        {
            if (Parent.DetailedEntity == null)
                return;

            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PostAsync(
                    requestUri: $"Sessions/Continue/{Parent.DetailedEntity.Id}",
                    content: null
                );


            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

            Parent.Refresh();
            CloseSessionStartForm();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    [RelayCommand]
    private async Task StopSession()
    {
        try
        {
            if (Parent.DetailedEntity == null)
                return;

            var stopSession = new StopSessionDto
            {
                EntityId = Parent.DetailedEntity.Id,
                Efficiency = EfficiencyId,
                Concentration = ConcentrationId,
                Summary = Summary
            };
            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PostAsJsonAsync( requestUri: $"Sessions/Stop", stopSession );
            
            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

            Parent.Refresh();
            CloseSessionStopForm();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    private async Task StartSessionAsync()
    {
        try
        {
            if (Parent.DetailedEntity == null)
                return;

            SessionDto dto = new()
            {
                EntityId = Parent.DetailedEntity.Id,
                PlannedDuration = PlannedMinutes.HasValue 
                    ? TimeSpan.FromMinutes(Convert.ToInt32(PlannedMinutes)) 
                    : null,
                Objective = Objective,
                StopAutomatically = StopAutomatically ?? false,
                AutoStopReason = AutoStopReason
            };
            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PostAsJsonAsync(requestUri: "Sessions/Start", dto);

            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

            Parent.Refresh();
            CloseSessionStartForm();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private async Task EndSessionAsync()
    {
        try
        {
            if (Parent.DetailedEntity == null)
                return;

            var dto = new PauseSessionDto(Parent.DetailedEntity.Id, PauseReason);
            
            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PostAsJsonAsync(requestUri: "Sessions/Pause", dto);

            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

            Parent.Refresh();
            CloseSessionPauseForm();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    private async Task AddDescriptionAsync()
    {
        try
        {
            var trimmedDescription = Description?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedDescription) || !EntityDetail.SelectedEntity.HasValue)
            {
                return;
            }
            
            var dto = new DescriptionDto()
            {
                EntityId = (Guid)EntityDetail.SelectedEntity, 
                Content = trimmedDescription
            };
            
            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PostAsJsonAsync(requestUri: "api/descriptions", dto);
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Server returned {response.StatusCode}: {body}");
            }
            else
            {
                Parent.Refresh();
                ResetDescription();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    private async Task DeleteEntity()
    {
        try
        {
            var id = Parent.DetailedEntity!.Id;
            await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PatchAsync(requestUri: $"Entity/Delete?entityId={id}", content: null);
            
            Parent.Refresh(true, false);
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
            var id = Parent.DetailedEntity!.Id;
            await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PatchAsync(requestUri: $"Entity/Archive?entityId={id}", content: null);
            
            Parent.Refresh(true, false);
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

    [RelayCommand]
    private void ShowDeadlineForm()
    {
        IsDeadlineFormOpen = true;

        var existingUtc = Parent.DetailedEntity?.Item?.ScheduledDeadline?.DeadlineAt;
        var local = existingUtc?.ToLocalTime() ?? DateTimeOffset.Now;

        DeadlineDate = local.Date;
        DeadlineTime = local.TimeOfDay;
    }

    [RelayCommand]
    private void CloseDeadlineForm()
    {
        IsDeadlineFormOpen = false;
    }

    [RelayCommand]
    private async Task UpsertDeadline()
    {
        try
        {
            if (Parent.DetailedEntity?.Item is null || DeadlineDate is null || DeadlineTime is null)
                return;

            var localDateTime = DeadlineDate.Value.Date + DeadlineTime.Value;
            var localOffset = TimeZoneInfo.Local.GetUtcOffset(localDateTime);
            var deadlineAt = new DateTimeOffset(localDateTime, localOffset);

            var dto = new ScheduleDeadlineDto(DeadlineAt: deadlineAt);

            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PutAsJsonAsync(
                    requestUri: $"items/{Parent.DetailedEntity.Item.EntityId}/deadline",
                    dto
                );

            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

            Parent.Refresh();
            CloseDeadlineForm();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    private async Task RemoveDeadline()
    {
        try
        {
            if (Parent.DetailedEntity?.Item is null)
                return;

            var itemId = Parent.DetailedEntity.Item.EntityId;

            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.DeleteAsync($"items/{itemId}/deadline");

            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

            // Parent.Refresh();
            CloseDeadlineForm();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

}