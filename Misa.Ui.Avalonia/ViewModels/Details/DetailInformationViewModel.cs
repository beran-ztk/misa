using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Reactive;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Audit;
using Misa.Contract.Audit.Lookups;
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

    [ObservableProperty] private bool isStartFormOpen;
    [ObservableProperty] private bool isPauseFormOpen;
    
    [ObservableProperty] private int plannedMinutes;
    [ObservableProperty] private string objective;
    [ObservableProperty] private bool stopAutomatically;
    [ObservableProperty] private string autoStopReason;
    
    [ObservableProperty] private int efficiency;
    [ObservableProperty] private int concentration;
    [ObservableProperty] private string summary;


    [RelayCommand]
    private void ShowSessionStartForm() => IsStartFormOpen = !IsStartFormOpen;
    [RelayCommand]
    private void CloseSessionStartForm() => IsStartFormOpen = false;

    [RelayCommand]
    private void ShowSessionPauseForm() => IsPauseFormOpen = !IsPauseFormOpen;

    [RelayCommand]
    private void CloseSessionPauseForm() => IsPauseFormOpen = false;
    
    [ObservableProperty] private int efficiencyId; 
    [ObservableProperty] private int concentrationId; 
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
                PlannedDuration = TimeSpan.FromMinutes(plannedMinutes),
                Objective = Objective,
                StopAutomatically = StopAutomatically,
                AutoStopReason = AutoStopReason
            };
            var response = await Parent.NavigationService.NavigationStore
                .MisaHttpClient.PostAsJsonAsync(requestUri: "sessions/start", dto);

            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");

            Parent.Refresh();
            IsStartFormOpen = false;
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

            if (string.IsNullOrWhiteSpace(trimmedDescription) || EntityDetail.SelectedEntity == null)
            {
                return;
            }
            
            var dto = new DescriptionDto()
            {
                EntityId = EntityDetail.SelectedEntity.Id, 
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