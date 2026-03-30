using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Misa.App.Shell.Components;

public partial class NavigationView : UserControl
{
    public NavigationView()
    {
        InitializeComponent();
    }

    private async void ExpansionStateChanged(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not TreeViewItem { DataContext: IndexEntry } entry) return;

        var expanded = e.RoutedEvent == TreeViewItem.ExpandedEvent;
    }
}