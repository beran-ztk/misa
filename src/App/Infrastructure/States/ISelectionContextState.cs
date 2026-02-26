using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public interface ISelectionContextState : INotifyPropertyChanged
{
    Guid? ActiveEntityId { get; }
    int UpdatedVersion { get; set; }
    int RemovedVersion { get; set; }
    void NotifyUpdated();
    void NotifyRemoved();
    void Set(Guid? id);
    void Clear();
}

public sealed partial class SelectionContextState : ObservableObject, ISelectionContextState
{
    [ObservableProperty] private Guid? _activeEntityId;
    [ObservableProperty] private bool _updated;
    [ObservableProperty] private bool _removed;

    [ObservableProperty] private int _updatedVersion;
    [ObservableProperty] private int _removedVersion;
    public void NotifyUpdated()
    {
        UpdatedVersion++;
    }

    public void NotifyRemoved()
    {
        RemovedVersion++;
    }

    public void Set(Guid? id) => ActiveEntityId = id;
    public void Clear() => ActiveEntityId = null;
}