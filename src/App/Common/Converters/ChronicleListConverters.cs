using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Misa.Contract.Items.Components.Chronicle;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class ChronicleTypeToChipTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString()?.ToUpperInvariant() ?? string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ChronicleTypeToChipBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChronicleEntryType t) return Brushes.Transparent;

        // sehr subtile Hintergründe
        return t switch
        {
            ChronicleEntryType.Session  => new SolidColorBrush(Color.FromArgb(0x14, 0x22, 0xC5, 0x5E)),
            ChronicleEntryType.Deadline => new SolidColorBrush(Color.FromArgb(0x14, 0xF9, 0x73, 0x16)),
            ChronicleEntryType.Journal  => new SolidColorBrush(Color.FromArgb(0x14, 0x3B, 0x82, 0xF6)),
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ChronicleTypeToChipBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChronicleEntryType t) return Brushes.Transparent;

        // etwas stärker als Background, aber immer noch dezent
        return t switch
        {
            ChronicleEntryType.Session  => new SolidColorBrush(Color.FromArgb(0x30, 0x22, 0xC5, 0x5E)),
            ChronicleEntryType.Deadline => new SolidColorBrush(Color.FromArgb(0x30, 0xF9, 0x73, 0x16)),
            ChronicleEntryType.Journal  => new SolidColorBrush(Color.FromArgb(0x30, 0x3B, 0x82, 0xF6)),
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}