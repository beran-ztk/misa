using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class PriorityToBrushConverter : IValueConverter
{
    public static readonly PriorityToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PriorityContract priority)
            return Brushes.Transparent;

        return priority switch
        {
            PriorityContract.None     => Brushes.Transparent,

            PriorityContract.Low      => new SolidColorBrush(Color.Parse("#9AA5B1")), // muted gray-blue
            PriorityContract.Medium   => new SolidColorBrush(Color.Parse("#5B8DEF")), // calm blue
            PriorityContract.High     => new SolidColorBrush(Color.Parse("#E2A03F")), // soft amber
            PriorityContract.Urgent   => new SolidColorBrush(Color.Parse("#E06C3C")), // warm orange
            PriorityContract.Critical => new SolidColorBrush(Color.Parse("#C44536")), // deep red

            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}