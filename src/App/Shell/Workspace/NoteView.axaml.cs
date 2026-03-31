using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using AvaloniaEdit.Editing;

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
        
        ConfigureEditor();
    }

    private void ConfigureEditor()
    {
        Editor.TextArea.SelectionBrush        = new SolidColorBrush(Color.Parse("#2A4A90E2"));
        Editor.TextArea.SelectionCornerRadius = 0; // otherwise selected area will be rounded and it looks weird on multiline
        
        Editor.TextArea.Background = Brushes.Transparent;

        Editor.TextArea.Margin = new Thickness(0, 32, 0, 0);

        Editor.TextArea.TextView.Margin = new Thickness(0, 0, 24, 0);

        // Remove the separator line inserted by ShowLineNumbers=True
        var separator = Editor.TextArea.LeftMargins
            .OfType<Line>()
            .FirstOrDefault();
        if (separator is not null)
            Editor.TextArea.LeftMargins.Remove(separator);

        // Style the line-number gutter: spacing + subtle opacity
        var lineNumbers = Editor.TextArea.LeftMargins
            .OfType<LineNumberMargin>()
            .FirstOrDefault();
        if (lineNumbers is not null)
        {
            lineNumbers.Margin  = new Thickness(16, 0, 14, 0);
            lineNumbers.Opacity = 0.45;
        }
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
