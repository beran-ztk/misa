using System.Collections.Generic;
using System.Linq;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Audits.Changes;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Activity;

public sealed partial class InspectorActivityViewModel : ViewModelBase
{
    private readonly InspectorFacadeViewModel _facade;

    /// <summary>Whether the Sessions section should be shown. Only true for real activity items (Task).</summary>
    public bool ShowSessions => _facade.State.IsRealActivity;

    public IReadOnlyList<Session> Sessions =>
        (_facade.State.Item.Activity?.Sessions ?? (IEnumerable<Session>)[])
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToList();

    public bool HasSessions => Sessions.Count > 0;

    public ICollection<AuditChange> Changes => _facade.State.Item.Changes;

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
