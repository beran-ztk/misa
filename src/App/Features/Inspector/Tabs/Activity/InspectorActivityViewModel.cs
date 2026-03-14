using System.Collections.Generic;
using System.Linq;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Activity;

public sealed partial class InspectorActivityViewModel : ViewModelBase
{
    private readonly InspectorFacadeViewModel _facade;

    public IReadOnlyList<SessionDto> Sessions =>
        (_facade.State.Item.Activity?.Sessions ?? (IEnumerable<SessionDto>)[])
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToList();

    public bool HasSessions => Sessions.Count > 0;

    public InspectorActivityViewModel(InspectorFacadeViewModel facade)
    {
        _facade = facade;

        facade.State.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InspectorState.Item))
            {
                OnPropertyChanged(nameof(Sessions));
                OnPropertyChanged(nameof(HasSessions));
            }
        };
    }
}
