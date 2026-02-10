using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using SessionViewModel = Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Extensions.Sessions.SessionViewModel;

namespace Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Base;

public partial class InspectorOverViewModel : ViewModelBase
{
    public InspectorFacadeViewModel Parent { get; }
    public SessionViewModel Session { get; }
    public InspectorOverViewModel(InspectorFacadeViewModel parent)
    {
        Parent = parent;
        Session = new SessionViewModel(this);
    }
}