using System;
using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Details;

public partial class DetailSessionViewModel : ViewModelBase
{
    public DetailMainDetailViewModel Parent { get; }
    public IEntityDetail EntityDetail { get; }
    

    public DetailSessionViewModel(DetailMainDetailViewModel parent)
    {
        Parent = parent;
        EntityDetail = parent.EntityDetail;
    }
    

}