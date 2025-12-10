using Misa.Contract.Entities;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Items;
using Misa.Ui.Avalonia.ViewModels.Shells;

namespace Misa.Ui.Avalonia.ViewModels.Entities;

public class EntityInformationViewModel : ViewModelBase
{
    public IEntityDetail EntityDetail { get; }

    public EntityInformationViewModel(EntityMainDetailViewModel parent)
    {
        EntityDetail = parent.EntityDetail;
    }
}