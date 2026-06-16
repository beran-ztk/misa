using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace Misa.Views;

public partial class MultiSelectFilterControl : UserControl
{
    private readonly StackPanel _itemsPanel = new() { Spacing = 2 };
    private readonly TextBlock _label = new() { TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly HashSet<string> _selected = [];
    private readonly List<(string Name, CheckBox Cb)> _items = [];

    public string Placeholder { get; set; } = "All";
    public event EventHandler? SelectionChanged;
    public IReadOnlySet<string> SelectedItems => _selected;

    public MultiSelectFilterControl()
    {
        InitializeComponent();
        ToggleBtn.Content = _label;
        var flyout = (Flyout)ToggleBtn.Flyout!;
        flyout.Content = new ScrollViewer { MaxHeight = 200, MinWidth = 140, Content = _itemsPanel };
        UpdateText();
    }

    public void SetItems(IEnumerable<string> items)
    {
        _selected.Clear();
        _items.Clear();
        _itemsPanel.Children.Clear();
        foreach (var item in items)
        {
            var name = item;
            var cb = new CheckBox { Content = name };
            cb.IsCheckedChanged += (_, _) =>
            {
                if (cb.IsChecked == true) _selected.Add(name);
                else _selected.Remove(name);
                UpdateText();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };
            _items.Add((name, cb));
            _itemsPanel.Children.Add(cb);
        }
        UpdateText();
    }

    // Updates the count label shown next to each item without touching selection state.
    // Items with count 0 show just the name; items with count > 0 show "Name (count)".
    public void UpdateCounts(IReadOnlyDictionary<string, int> counts)
    {
        foreach (var (name, cb) in _items)
        {
            var count = counts.GetValueOrDefault(name, 0);
            cb.Content = count > 0 ? $"{name} ({count})" : name;
        }
    }

    private void UpdateText()
    {
        _label.Text = _selected.Count == 0
            ? Placeholder
            : string.Join(", ", _selected);
    }
}
