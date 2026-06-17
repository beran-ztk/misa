using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Music.Views;

internal static class MetadataChipFactory
{
    public static ToggleButton Create(string name, int count, bool isChecked = false)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(new TextBlock
        {
            Text = name,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });

        if (count > 0)
        {
            content.Children.Add(new Border
            {
                MinWidth = 22,
                Height = 16,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Child = new TextBlock
                {
                    Text = count.ToString(),
                    FontSize = 10,
                    Opacity = 0.82,
                    MinWidth = 22,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }

        var button = new ToggleButton
        {
            Content = content,
            IsChecked = isChecked
        };
        button.Classes.Add("metadata-chip");
        return button;
    }
}
