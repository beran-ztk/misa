using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.App.Infrastructure;
using Misa.Domain;

namespace Misa.App.Shell.Components;

public partial class IndexEntry : ObservableObject
{
    public Guid Id { get; init; }
    public Guid? ParentId  { get; init; }
    public Kind Kind  { get; init; }
    public string Title { get; init; } = string.Empty;
    public ObservableCollection<IndexEntry> Children { get; } = [];
}
public sealed class NavigationViewModel : ViewModelBase
{
    public ObservableCollection<IndexEntry> IndexEntries { get; } = [];
}