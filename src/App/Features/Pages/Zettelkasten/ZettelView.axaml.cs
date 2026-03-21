using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Editing;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class ZettelView : UserControl
{
    private ZettelViewModel? _vm;
    private bool             _syncing;

    public ZettelView()
    {
        InitializeComponent();

        Editor.TextChanged += OnEditorTextChanged;
        DataContextChanged += OnDataContextChanged;

        // Selection: accent-coloured highlight, text keeps its normal colour
        Editor.TextArea.SelectionBrush        = new SolidColorBrush(Color.Parse("#2A4A90E2"));
        Editor.TextArea.SelectionForeground   = null;   // inherit — no inversion
        Editor.TextArea.SelectionCornerRadius = 0;

        // Disable features that don't belong in a plain-text note editor
        Editor.Options.EnableHyperlinks            = false;
        Editor.Options.EnableEmailHyperlinks       = false;
        Editor.Options.HighlightCurrentLine        = false;
        Editor.Options.ShowBoxForControlCharacters = false;

        ConfigureEditor();
    }

    private void ConfigureEditor()
    {
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

    // ── DataContext lifecycle ─────────────────────────────────────────────────

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm = null;
        }

        if (DataContext is not ZettelViewModel vm) return;

        _vm = vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;

        // Defer until after the current binding cycle finishes.
        // DataContext and IsVisible are both driven by the same ZettelVm change on
        // the parent; IsVisible=true is applied synchronously right after DataContext,
        // so by the time this posted callback runs the editor is live and visible.
        Dispatcher.UIThread.Post(() => LoadEditorText(_vm?.Content), DispatcherPriority.Loaded);
    }

    // Fallback: if the editor is hidden when the deferred post runs (edge case),
    // reload as soon as it becomes visible.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == Visual.IsVisibleProperty && IsVisible && _vm is not null)
            LoadEditorText(_vm.Content);
    }

    // Sync Content → editor when it changes from outside (e.g. different zettel loaded)
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ZettelViewModel.Content)) return;
        if (_vm is null || Editor.Text == (_vm.Content ?? string.Empty)) return;
        LoadEditorText(_vm.Content);
    }

    // ── Editor → ViewModel ───────────────────────────────────────────────────

    private void OnEditorTextChanged(object? sender, System.EventArgs e)
    {
        if (_syncing || _vm is null) return;
        _vm.Content = Editor.Text;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void LoadEditorText(string? text)
    {
        _syncing    = true;
        Editor.Text = text ?? string.Empty;
        _syncing    = false;
    }
}
