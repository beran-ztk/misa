using System;
using System.ComponentModel;
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
    public INavigationService NavigationService { get; }
    
    public DetailMainDetailViewModel(IEntityDetail parent, INavigationService navigationService)
    {
        EntityDetail = parent;
        NavigationService = navigationService;

        InformationViewModel = new DetailInformationViewModel(this, true);
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

    public void Refresh(bool dataHasBeenChanged = false, bool reloadDetails = true)
    {
        if (dataHasBeenChanged)
            EntityDetail.ReloadList();

        if (!reloadDetails)
        {
            EntityDetail.SelectedEntity = null;
            DetailedEntity = null;
            return;
        }
            
        
        var id = EntityDetail.SelectedEntity;
        _ = OnSelectedEntityChanged(DetailedEntity?.Id);
    }
    private async Task OnSelectedEntityChanged(Guid? id = null)
    {
        Guid entityId;
        
        if (EntityDetail.SelectedEntity != null)
            entityId = (Guid)EntityDetail.SelectedEntity;
        else if (id != null)
            entityId = (Guid)id;
        else
            return;
        
        try
        {
            var response = await NavigationService.NavigationStore.MisaHttpClient
                .GetFromJsonAsync<EntityDto>(requestUri: $"api/entities/{entityId}");
            
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