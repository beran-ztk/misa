using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Music.Views;

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

        // Build button content: label (left) + dropdown arrow (right)
        var arrow = new TextBlock
        {
            Text = "▾",
            Opacity = 0.45,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
        };
        var container = new Grid();
        container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(_label, 0);
        Grid.SetColumn(arrow, 1);
        container.Children.Add(_label);
        container.Children.Add(arrow);
        ToggleBtn.Content = container;

        var flyout = (Flyout)ToggleBtn.Flyout!;
        flyout.Content = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(13, 21, 29)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(49, 75, 95)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = new ScrollViewer { MaxHeight = 280, MinWidth = 220, Content = _itemsPanel }
        };
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
            var cb = new CheckBox
            {
                Content = name,
                Foreground = new SolidColorBrush(Color.FromRgb(236, 243, 249)),
                Padding = new Thickness(4, 2)
            };
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
