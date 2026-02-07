using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public interface ISelectionContextState : INotifyPropertyChanged
{
    Guid? ActiveEntityId { get; }
    void SetActive(Guid? id);
    void Clear();
}

public sealed partial class SelectionContextState : ObservableObject, ISelectionContextState
{
    [ObservableProperty] private Guid? _activeEntityId;

    public void SetActive(Guid? id) => ActiveEntityId = id;
    public void Clear() => ActiveEntityId = null;
}