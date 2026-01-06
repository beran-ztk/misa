using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Entities;
using Misa.Ui.Avalonia.App.Shell;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;
using ReactiveUI;

namespace Misa.Ui.Avalonia.Features.Details;

public class DetailMainDetailViewModel : ViewModelBase
{
    public IEntityDetail EntityDetail { get; }
    public EntityDto? _detailedEntity;
    private int _selectedTabIndex;
    public DetailInformationViewModel InformationViewModel { get; }
    public INavigationService NavigationService { get; }
    
    public DetailMainDetailViewModel(IEntityDetail parent, INavigationService navigationService)
    {
        EntityDetail = parent;
        NavigationService = navigationService;

        InformationViewModel = new DetailInformationViewModel(this);
        
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