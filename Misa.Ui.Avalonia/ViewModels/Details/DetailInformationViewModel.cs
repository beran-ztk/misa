using Misa.Contract.Entities;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Items;
using Misa.Ui.Avalonia.ViewModels.Shells;

namespace Misa.Ui.Avalonia.ViewModels.Details;

public class DetailInformationViewModel : ViewModelBase
{
    public IEntityDetail EntityDetail { get; }

    public DetailInformationViewModel(DetailMainDetailViewModel parent)
    {
        EntityDetail = parent.EntityDetail;
    }
}