using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Resona.Models;

namespace Resona.Views;

internal sealed record GenreMetadataTooltipEntry(
    ModelSubgenre Subgenre,
    string MainGenreName,
    IReadOnlyList<ModelSubgenreDistinction> Distinctions);

internal static class GenreMetadataTooltipFactory
{
    public static Control Create(IEnumerable<GenreMetadataTooltipEntry> source)
    {
        var entries = source.ToList();
        var content = new StackPanel
        {
            Width = 410,
            Spacing = 15
        };

        for (var index = 0; index < entries.Count; index++)
        {
            if (index > 0)
                content.Children.Add(new Rectangle
                {
                    Height = 1,
                    Fill = ThemeResources.Brush("Theme.Brush.Divider")
                });
            content.Children.Add(CreateEntry(entries[index]));
        }

        return new ScrollViewer
        {
            Width = 414,
            MaxHeight = 430,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
    }

    private static Control CreateEntry(GenreMetadataTooltipEntry entry)
    {
        var subgenre = entry.Subgenre;
        var accent = MainGenrePalette.For(entry.MainGenreName);
        var panel = new StackPanel { Spacing = 11 };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 18
        };
        header.Children.Add(new TextBlock
        {
            Text = subgenre.Name,
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            Foreground = accent,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        if (subgenre.BpmMin is not null || subgenre.BpmMax is not null)
        {
            var bpm = new StackPanel
            {
                Spacing = 1,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            bpm.Children.Add(new TextBlock
            {
                Text = "TYPICAL BPM",
                FontSize = 8.5,
                FontWeight = FontWeight.SemiBold,
                LetterSpacing = 0.65,
                Opacity = 0.46,
                HorizontalAlignment = HorizontalAlignment.Right
            });
            bpm.Children.Add(new TextBlock
            {
                Text = FormatBpm(subgenre),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = ThemeResources.Brush("Theme.Brush.TextPrimary"),
                HorizontalAlignment = HorizontalAlignment.Right
            });
            Grid.SetColumn(bpm, 1);
            header.Children.Add(bpm);
        }
        panel.Children.Add(header);

        if (!string.IsNullOrWhiteSpace(subgenre.Description))
            panel.Children.Add(new TextBlock
            {
                Text = subgenre.Description,
                FontSize = 10.5,
                Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
                Opacity = 0.84,
                TextWrapping = TextWrapping.Wrap
            });

        if (!string.IsNullOrWhiteSpace(subgenre.ClassificationHint))
        {
            var guidance = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("3,*"),
                ColumnSpacing = 10
            };
            guidance.Children.Add(new Border
            {
                Width = 3,
                CornerRadius = new CornerRadius(2),
                Background = accent
            });
            var guidanceText = new StackPanel { Spacing = 3 };
            guidanceText.Children.Add(new TextBlock
            {
                Text = "USE WHEN",
                FontSize = 8.5,
                FontWeight = FontWeight.SemiBold,
                LetterSpacing = 0.7,
                Foreground = accent
            });
            guidanceText.Children.Add(new TextBlock
            {
                Text = subgenre.ClassificationHint,
                FontSize = 10.5,
                Foreground = ThemeResources.Brush("Theme.Brush.TextPrimary"),
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(guidanceText, 1);
            guidance.Children.Add(guidanceText);
            panel.Children.Add(guidance);
        }

        if (entry.Distinctions.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "DISTINGUISH FROM",
                FontSize = 8.5,
                FontWeight = FontWeight.SemiBold,
                LetterSpacing = 0.7,
                Opacity = 0.48,
                Margin = new Thickness(0, 2, 0, 0)
            });
            var distinctions = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemWidth = 200
            };
            foreach (var distinction in entry.Distinctions)
            {
                var distinctionText = new StackPanel { Spacing = 3 };
                distinctionText.Children.Add(new TextBlock
                {
                    Text = distinction.ModelSubgenreName,
                    FontSize = 10.5,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = MainGenrePalette.For(distinction.ModelGenreName),
                    TextWrapping = TextWrapping.Wrap
                });
                distinctionText.Children.Add(new TextBlock
                {
                    Text = distinction.Difference,
                    FontSize = 9.5,
                    Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
                    Opacity = 0.78,
                    TextWrapping = TextWrapping.Wrap
                });
                distinctions.Children.Add(new Border
                {
                    Width = 194,
                    Margin = new Thickness(0, 0, 6, 10),
                    Padding = new Thickness(9, 0, 5, 0),
                    BorderBrush = MainGenrePalette.For(distinction.ModelGenreName),
                    BorderThickness = new Thickness(2, 0, 0, 0),
                    Child = distinctionText
                });
            }
            panel.Children.Add(distinctions);
        }

        return panel;
    }

    private static string FormatBpm(ModelSubgenre subgenre) =>
        subgenre.BpmMin is not null && subgenre.BpmMax is not null
            ? $"{subgenre.BpmMin}–{subgenre.BpmMax}"
            : subgenre.BpmMin is not null
                ? $"from {subgenre.BpmMin}"
                : $"up to {subgenre.BpmMax}";
}
