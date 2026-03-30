using System;
using System.ComponentModel;
using Avalonia.Controls;

namespace Misa.App.Shell.Workspace;

public partial class NoteView : UserControl
{
    private NoteViewModel? _vm;
    private bool _syncing;

    public NoteView()
    {
        InitializeComponent();
        Editor.TextChanged += OnEditorTextChanged;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;

        _vm = DataContext as NoteViewModel;
        if (_vm is null) return;

        _vm.PropertyChanged += OnViewModelPropertyChanged;

        // Populate the editor without triggering autosave
        _syncing = true;
        Editor.Text = _vm.Content ?? string.Empty;
        _syncing = false;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NoteViewModel.Content) || _syncing) return;
        _syncing = true;
        Editor.Text = _vm!.Content ?? string.Empty;
        _syncing = false;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_syncing || _vm is null) return;
        _syncing = true;
        _vm.Content = Editor.Text;
        _syncing = false;
    }
}
