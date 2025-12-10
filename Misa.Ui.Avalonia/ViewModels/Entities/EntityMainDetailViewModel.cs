using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Items;
using Misa.Ui.Avalonia.ViewModels.Shells;

namespace Misa.Ui.Avalonia.ViewModels.Entities;

public class EntityMainDetailViewModel : ViewModelBase
{
    public IEntityDetail EntityDetail { get; }
    private int _selectedTabIndex;
    public EntityInformationViewModel InformationViewModel { get; }
    public EntityInformationViewModel RelationViewModel { get; }
    public EntityInformationViewModel SessionViewModel { get; }
    public EntityInformationViewModel HistoryViewModel { get; }
    public EntityMainDetailViewModel(IEntityDetail parent)
    {
        EntityDetail = parent;

        InformationViewModel = new EntityInformationViewModel(this);
        RelationViewModel = new EntityInformationViewModel(this);
        SessionViewModel = new EntityInformationViewModel(this);
        HistoryViewModel = new EntityInformationViewModel(this);
    }
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    private void LoadDetailsAsync()
    {
        if (EntityDetail.SelectedEntity == null)
        {
            return;
        }
        
        if (EntityDetail.SelectedEntity.Workflow.Id == 1)
        {
            
        }
    }
}