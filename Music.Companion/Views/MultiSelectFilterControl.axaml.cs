using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Music.Companion.Views;

public partial class MultiSelectFilterControl : UserControl
{
    private readonly StackPanel _itemsPanel = new() { Spacing = 2 };
    private readonly TextBlock _label = new() { TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Name, CheckBox CheckBox)> _items = [];

    public string Placeholder { get; set; } = "All";
    public event EventHandler? SelectionChanged;
    public IReadOnlySet<string> SelectedItems => _selected;

    public MultiSelectFilterControl()
    {
        InitializeComponent();

        var arrow = new TextBlock
        {
            Text = "v",
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
        flyout.Content = new ScrollViewer
        {
            MaxHeight = 320,
            MinWidth = 250,
            Padding = new Thickness(6),
            Content = _itemsPanel
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
            var checkBox = new CheckBox { Content = name };
            checkBox.IsCheckedChanged += (_, _) =>
            {
                if (checkBox.IsChecked == true)
                    _selected.Add(name);
                else
                    _selected.Remove(name);

                UpdateText();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };

            _items.Add((name, checkBox));
            _itemsPanel.Children.Add(checkBox);
        }

        UpdateText();
    }

    public void UpdateCounts(IReadOnlyDictionary<string, int> counts)
    {
        foreach (var (name, checkBox) in _items)
        {
            var count = counts.GetValueOrDefault(name, 0);
            checkBox.Content = count > 0 ? $"{name} ({count})" : name;
        }
    }

    private void UpdateText()
    {
        _label.Text = _selected.Count == 0
            ? Placeholder
            : string.Join(", ", _selected.Order(StringComparer.OrdinalIgnoreCase));
    }
}
