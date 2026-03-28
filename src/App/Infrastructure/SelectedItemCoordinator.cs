using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Misa.Ui.Avalonia.Infrastructure;

public sealed partial class SelectedItemCoordinator : ObservableObject
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