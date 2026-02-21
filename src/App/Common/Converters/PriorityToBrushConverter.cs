using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Misa.Contract.Items.Components.Activity;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class PriorityToBrushConverter : IValueConverter
{
    public static readonly PriorityToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ActivityPriorityDto priority)
            return Brushes.Transparent;

        return priority switch
        {
            ActivityPriorityDto.None     => Brushes.Transparent,

            ActivityPriorityDto.Low      => new SolidColorBrush(Color.Parse("#9AA5B1")), // muted gray-blue
            ActivityPriorityDto.Medium   => new SolidColorBrush(Color.Parse("#5B8DEF")), // calm blue
            ActivityPriorityDto.High     => new SolidColorBrush(Color.Parse("#E2A03F")), // soft amber
            ActivityPriorityDto.Urgent   => new SolidColorBrush(Color.Parse("#E06C3C")), // warm orange
            ActivityPriorityDto.Critical => new SolidColorBrush(Color.Parse("#C44536")), // deep red

            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}