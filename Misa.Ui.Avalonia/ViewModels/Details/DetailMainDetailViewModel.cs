using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using Misa.Contract.Entities;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.Services.Navigation;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Items;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Details;

public class DetailMainDetailViewModel : ViewModelBase
{
    public IEntityDetail EntityDetail { get; }
    public EntityDto? _detailedEntity;
    public ViewModelBase? EntityInformationView { get; set; }
    private int _selectedTabIndex;
    public DetailInformationViewModel InformationViewModel { get; }
    public DetailInformationViewModel RelationViewModel { get; }
    public DetailInformationViewModel SessionViewModel { get; }
    public DetailInformationViewModel HistoryViewModel { get; }
    public INavigationService NavigationService;
    
    public DetailMainDetailViewModel(IEntityDetail parent, INavigationService navigationService)
    {
        EntityDetail = parent;
        NavigationService = navigationService;

        InformationViewModel = new DetailInformationViewModel(this);
        RelationViewModel = new DetailInformationViewModel(this);
        SessionViewModel = new DetailInformationViewModel(this);
        HistoryViewModel = new DetailInformationViewModel(this);
        
        this.WhenAnyValue(x => x.EntityDetail.SelectedEntity)
            .Subscribe(_ => OnSelectedEntityChanged());

    }
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }
    public EntityDto? DetailedEntity
    {
        get => _detailedEntity;
        set
        {
            SetProperty(ref _detailedEntity, value);
            OnPropertyChanged(nameof(HasDetailedEntity));
        }
    }

    public bool HasDetailedEntity => DetailedEntity is not null;

    public void Refresh()
    {
        _ = OnSelectedEntityChanged();
    }
    private async Task OnSelectedEntityChanged()
    {
        if (EntityDetail.SelectedEntity == null)
        {
            return;
        }
        
        try
        {
            var response = await NavigationService.NavigationStore.MisaHttpClient
                .GetFromJsonAsync<EntityDto>(requestUri: $"api/entities/{EntityDetail.SelectedEntity.Id}");
            
            if (response == null)
                return;
            DetailedEntity = response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}