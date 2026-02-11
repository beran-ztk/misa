using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel : ViewModelBase
{
    public InspectorFacadeViewModel Facade { get; }

    public InspectorEntryViewModel(InspectorFacadeViewModel facade)
    {
        Facade = facade;

        Facade.State.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Facade.State.CurrentSessionOverview))
            {
                OnPropertyChanged(nameof(CurrentSession));
                OnPropertyChanged(nameof(HasActiveSession));
            }
        };
    }
}