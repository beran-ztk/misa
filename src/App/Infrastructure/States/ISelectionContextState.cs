using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public interface ISelectionContextState : INotifyPropertyChanged
{
    Guid? ActiveEntityId { get; }
    void Set(Guid? id);
    void Clear();
}

public sealed partial class SelectionContextState : ObservableObject, ISelectionContextState
{
    [ObservableProperty] private Guid? _activeEntityId;

    public void Set(Guid? id) => ActiveEntityId = id;
    public void Clear() => ActiveEntityId = null;
}