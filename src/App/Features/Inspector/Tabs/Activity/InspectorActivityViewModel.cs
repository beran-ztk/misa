using System.Collections.Generic;
using System.Linq;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Items.Components.Audits;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Activity;

public sealed partial class InspectorActivityViewModel : ViewModelBase
{
    private readonly InspectorFacadeViewModel _facade;

    /// <summary>Whether the Sessions section should be shown. Only true for real activity items (Task).</summary>
    public bool ShowSessions => _facade.State.IsRealActivity;

    public IReadOnlyList<SessionDto> Sessions =>
        (_facade.State.Item.Activity?.Sessions ?? (IEnumerable<SessionDto>)[])
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToList();

    public bool HasSessions => Sessions.Count > 0;

    public IReadOnlyList<AuditChangeDto> Changes =>
        _facade.State.Item.Changes;

    public bool HasChanges => Changes.Count > 0;

    public InspectorActivityViewModel(InspectorFacadeViewModel facade)
    {
        _facade = facade;

        facade.State.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InspectorState.Item))
            {
                OnPropertyChanged(nameof(ShowSessions));
                OnPropertyChanged(nameof(Sessions));
                OnPropertyChanged(nameof(HasSessions));
                OnPropertyChanged(nameof(Changes));
                OnPropertyChanged(nameof(HasChanges));
            }
        };
    }
}
