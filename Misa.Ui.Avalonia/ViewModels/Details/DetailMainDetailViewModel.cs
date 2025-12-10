using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Items;
using Misa.Ui.Avalonia.ViewModels.Shells;

namespace Misa.Ui.Avalonia.ViewModels.Details;

public class DetailMainDetailViewModel : ViewModelBase
{
    public IEntityDetail EntityDetail { get; }
    private int _selectedTabIndex;
    public DetailInformationViewModel InformationViewModel { get; }
    public DetailInformationViewModel RelationViewModel { get; }
    public DetailInformationViewModel SessionViewModel { get; }
    public DetailInformationViewModel HistoryViewModel { get; }
    public DetailMainDetailViewModel(IEntityDetail parent)
    {
        EntityDetail = parent;

        InformationViewModel = new DetailInformationViewModel(this);
        RelationViewModel = new DetailInformationViewModel(this);
        SessionViewModel = new DetailInformationViewModel(this);
        HistoryViewModel = new DetailInformationViewModel(this);
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