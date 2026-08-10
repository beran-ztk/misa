using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Resona.Companion.Views;

public partial class MultiSelectFilterControl : UserControl
{
    private readonly StackPanel _itemsPanel = new() { Spacing = 2 };
    private readonly TextBlock _label = new() { TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly Border _flyoutContent;
    private readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FilterItem> _items = [];
    private bool _updatingSelection;

    private sealed record FilterItem(string Name, Control Row, CheckBox CheckBox, TextBlock CountText, Border CountBadge);

    public string Placeholder { get; set; } = "All";
    public event EventHandler? SelectionChanged;
    public IReadOnlySet<string> SelectedItems => _selected;

    public MultiSelectFilterControl()
    {
        InitializeComponent();

        var arrow = new TextBlock
        {
            Text = "▾",
            Opacity = 0.45,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0)
        };

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        content.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(_label, 0);
        Grid.SetColumn(arrow, 1);
        content.Children.Add(_label);
        content.Children.Add(arrow);
        ToggleBtn.Content = content;

        var flyout = (Flyout)ToggleBtn.Flyout!;
        _flyoutContent = new Border
        {
            Background = CompanionTheme.Brush("Mobile.Brush.SurfaceRaised"),
            BorderBrush = CompanionTheme.Brush("Mobile.Brush.BorderStrong"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10, 8),
            Child = new ScrollViewer { MaxHeight = 280, MinWidth = 220, Content = _itemsPanel }
        };
        flyout.Content = _flyoutContent;
        flyout.Opening += (_, _) => _flyoutContent.Width = ToggleBtn.Bounds.Width;

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
            var checkBox = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0)
            };
            checkBox.IsCheckedChanged += (_, _) =>
            {
                if (checkBox.IsChecked == true)
                    _selected.Add(name);
                else
                    _selected.Remove(name);

                UpdateText();
                if (!_updatingSelection)
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
            };

            var nameText = new TextBlock
            {
                Text = name,
                Foreground = CompanionTheme.Brush("Mobile.Brush.TextPrimary"),
                FontWeight = FontWeight.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(8, 0, 10, 0)
            };

            var countText = new TextBlock
            {
                Foreground = CompanionTheme.Brush("Mobile.Brush.TextSecondary"),
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
                Margin = new Thickness(0, 0, 12, 0),
                CornerRadius = new CornerRadius(9),
                Background = CompanionTheme.Brush("Mobile.Brush.AccentSurface"),
                BorderBrush = CompanionTheme.Brush("Mobile.Brush.Border"),
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

                checkBox.IsChecked = checkBox.IsChecked != true;
            };

            Grid.SetColumn(checkBox, 0);
            Grid.SetColumn(nameText, 1);
            Grid.SetColumn(countBadge, 2);
            row.Children.Add(checkBox);
            row.Children.Add(nameText);
            row.Children.Add(countBadge);

            _items.Add(new FilterItem(name, row, checkBox, countText, countBadge));
            _itemsPanel.Children.Add(row);
        }

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
            .OrderByDescending(item => counts.GetValueOrDefault(item.Name, 0))
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _itemsPanel.Children.Clear();
        foreach (var item in sortedItems)
            _itemsPanel.Children.Add(item.Row);
    }

    private void UpdateText()
    {
        _label.Text = _selected.Count == 0
            ? Placeholder
            : string.Join(", ", _items
                .Where(item => _selected.Contains(item.Name))
                .Select(item => item.Name));
    }
}
