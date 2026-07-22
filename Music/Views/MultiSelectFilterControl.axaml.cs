using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Music.Views;

public partial class MultiSelectFilterControl : UserControl
{
    private readonly StackPanel _itemsPanel = new() { Spacing = 2 };
    private readonly TextBlock _label = new() { TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly Border _flyoutContent;
    private readonly HashSet<string> _selected = [];
    private readonly List<FilterItem> _items = [];
    private bool _updatingSelection;

    public sealed record FilterOption(
        string Value,
        string DisplayName,
        string? GroupName = null,
        string? GroupColor = null);

    private sealed record FilterItem(
        string Name,
        string DisplayName,
        string GroupName,
        string? GroupColor,
        int GroupIndex,
        Control Row,
        CheckBox CheckBox,
        TextBlock CountText,
        Border CountBadge);

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
        _flyoutContent = new Border
        {
            Background = ThemeResources.Brush("Theme.Brush.Surface"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderStrong"),
            BorderThickness = new Thickness(1,0,1,1),
            CornerRadius = new CornerRadius(0,0,6,6),
            Padding = new Thickness(10, 8),
            Child = new ScrollViewer { MaxHeight = 280, MinWidth = 220, Content = _itemsPanel }
        };
        flyout.Content = _flyoutContent;
        flyout.Opening += (_, _) => _flyoutContent.Width = ToggleBtn.Bounds.Width;

        UpdateText();
    }

    public void SetItems(IEnumerable<string> items)
    {
        SetItems(items.Select(item => new FilterOption(item, item)));
    }

    public void SetItems(IEnumerable<FilterOption> items)
    {
        _selected.Clear();
        _items.Clear();
        _itemsPanel.Children.Clear();
        var groupIndexes = new Dictionary<(string Name, string? Color), int>();
        foreach (var item in items)
        {
            var name = item.Value;
            var groupName = item.GroupName ?? string.Empty;
            var groupKey = (groupName, item.GroupColor);
            if (!groupIndexes.TryGetValue(groupKey, out var groupIndex))
                groupIndexes[groupKey] = groupIndex = groupIndexes.Count;
            var cb = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0)
            };
            cb.IsCheckedChanged += (_, _) =>
            {
                if (cb.IsChecked == true) _selected.Add(name);
                else _selected.Remove(name);
                UpdateText();
                if (!_updatingSelection)
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
            };

            var nameText = new TextBlock
            {
                Text = item.DisplayName,
                Foreground = ThemeResources.Brush("Theme.Brush.TextPrimary"),
                FontWeight = FontWeight.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(8, 0, 10, 0)
            };

            var countText = new TextBlock
            {
                Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
                FontSize = 11,
                FontWeight = FontWeight.Medium,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var countBadge = new Border
            {
                IsVisible = false,
                MinWidth = 24,
                Padding = new Thickness(7, 1),
                Margin = new Thickness(0,0,12,0),
                CornerRadius = new CornerRadius(9),
                Background = ThemeResources.Brush("Theme.Brush.SurfaceSelected"),
                BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
                BorderThickness = new Thickness(1),
                Child = countText,
                VerticalAlignment = VerticalAlignment.Center
            };

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                MinHeight = 30,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            row.PointerPressed += (_, e) =>
            {
                if (e.Source is CheckBox)
                    return;

                cb.IsChecked = cb.IsChecked != true;
            };

            Grid.SetColumn(cb, 0);
            Grid.SetColumn(nameText, 1);
            Grid.SetColumn(countBadge, 2);
            row.Children.Add(cb);
            row.Children.Add(nameText);
            row.Children.Add(countBadge);

            _items.Add(new FilterItem(name, item.DisplayName, groupName, item.GroupColor, groupIndex, row, cb, countText, countBadge));
        }
        RebuildItemsPanel(_items);
        UpdateText();
    }

    public void SetSelectedItems(IEnumerable<string> selectedItems, bool notify = true)
    {
        var selected = selectedItems.ToHashSet(StringComparer.OrdinalIgnoreCase);

        _updatingSelection = true;
        _selected.Clear();
        foreach (var item in _items)
        {
            var isSelected = selected.Contains(item.Name);
            item.CheckBox.IsChecked = isSelected;
            if (isSelected)
                _selected.Add(item.Name);
        }
        _updatingSelection = false;

        UpdateText();
        if (notify)
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateCounts(IReadOnlyDictionary<string, int> counts)
    {
        foreach (var item in _items)
        {
            var count = counts.GetValueOrDefault(item.Name, 0);
            item.CountText.Text = count.ToString();
            item.CountBadge.IsVisible = count > 0;
        }

        var sortedItems = _items
            .OrderBy(item => item.GroupIndex)
            .ThenByDescending(item => counts.GetValueOrDefault(item.Name, 0))
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RebuildItemsPanel(sortedItems);
    }

    private void RebuildItemsPanel(IEnumerable<FilterItem> items)
    {
        _itemsPanel.Children.Clear();
        foreach (var group in items.GroupBy(item => item.GroupIndex))
        {
            var first = group.First();
            if (!string.IsNullOrWhiteSpace(first.GroupName))
            {
                var header = new TextBlock
                {
                    Text = first.GroupName.ToUpperInvariant(),
                    FontSize = 9.5,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = ToBrush(first.GroupColor),
                    Opacity = .88,
                    Margin = new Thickness(0, 7, 0, 2)
                };
                _itemsPanel.Children.Add(header);
            }

            foreach (var item in group)
                _itemsPanel.Children.Add(item.Row);
        }
    }

    private static IBrush ToBrush(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return ThemeResources.Brush("Theme.Brush.TextMuted");

        try { return new SolidColorBrush(Color.Parse(color)); }
        catch { return ThemeResources.Brush("Theme.Brush.TextMuted"); }
    }

    private void UpdateText()
    {
        _label.Text = _selected.Count == 0
            ? Placeholder
            : string.Join(", ", _items
                .Where(item => _selected.Contains(item.Name))
                .Select(item => item.DisplayName));
    }
}
