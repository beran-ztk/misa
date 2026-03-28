using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class PriorityToBrushConverter : IValueConverter
{
    public static readonly PriorityToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ActivityPriority priority)
            return Brushes.Transparent;

        return priority switch
        {
            ActivityPriority.None     => Brushes.Transparent,

            ActivityPriority.Low      => new SolidColorBrush(Color.Parse("#9AA5B1")), // muted gray-blue
            ActivityPriority.Medium   => new SolidColorBrush(Color.Parse("#5B8DEF")), // calm blue
            ActivityPriority.High     => new SolidColorBrush(Color.Parse("#E2A03F")), // soft amber
            ActivityPriority.Urgent   => new SolidColorBrush(Color.Parse("#E06C3C")), // warm orange
            ActivityPriority.Critical => new SolidColorBrush(Color.Parse("#C44536")), // deep red

            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}