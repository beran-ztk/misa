using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Reactive;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Audit;
using Misa.Contract.Audit.Lookups;
using Misa.Contract.Items;
using Misa.Contract.Items.Lookups;
using Misa.Contract.Main;
using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Details;

public partial class DetailInformationViewModel : ViewModelBase
{
    public IEntityDetail EntityDetail { get; }
    public DetailMainDetailViewModel Parent { get; }
    private string _description = string.Empty;
    public ReactiveCommand<Unit, Unit> AddDescriptionCommand { get; }
    public ReactiveCommand<Unit, Unit> StartSessionCommand { get; }
    public ReactiveCommand<Unit, Unit> PauseSessionCommand { get; }
    

    public DetailInformationViewModel(DetailMainDetailViewModel parent)
    {
        Parent = parent;
        EntityDetail = parent.EntityDetail;
        
        AddDescriptionCommand = ReactiveCommand.CreateFromTask(AddDescriptionAsync);
        AddDescriptionCommand.Subscribe();
        
        StartSessionCommand = ReactiveCommand.CreateFromTask(StartSessionAsync);
        StartSessionCommand.Subscribe();
        
        PauseSessionCommand = ReactiveCommand.CreateFromTask(EndSessionAsync);
        PauseSessionCommand.Subscribe();
        
        this.WhenAnyValue(x => x.EntityDetail.SelectedEntity)
            .Subscribe(_ => ResetDescription());
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
    
    [ObservableProperty] private int? plannedMinutes;
    [ObservableProperty] private string? objective;
    [ObservableProperty] private bool? stopAutomatically;
    [ObservableProperty] private string? autoStopReason;
    
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

        Parent.Refresh();
        IsEditTitleFormOpen = false;
    }

    [RelayCommand]
    private void ShowSessionStartForm()
    {
        IsStartFormOpen = !IsStartFormOpen;
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
        Summary = null;
        Efficiency = null;
        Concentration = null;
        EfficiencyId = null;
        ConcentrationId = null;
    } 

    [RelayCommand]
    private void CloseSessionPauseForm()
    {
        IsPauseFormOpen = false;
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
                .MisaHttpClient.PostAsJsonAsync(requestUri: "sessions/start", dto);

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

            SessionDto dto = new()
            {
                EntityId = Parent.DetailedEntity.Id,
                EfficiencyId = EfficiencyId,
                ConcentrationId = ConcentrationId,
                Summary = Summary,
            };
            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PostAsJsonAsync(requestUri: "sessions/pause", dto);

            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

            Parent.Refresh();
            IsPauseFormOpen = false;
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
}