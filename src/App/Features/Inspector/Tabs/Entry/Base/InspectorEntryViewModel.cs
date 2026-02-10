using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel : ViewModelBase
{
    public InspectorFacadeViewModel Facade { get; }
    public SessionViewModel Session { get; }
    public InspectorEntryViewModel(InspectorFacadeViewModel facade)
    {
        Facade = facade;
        Session = new SessionViewModel(Facade);
    }
}