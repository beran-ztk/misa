using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Misa.Core.Features.Items.Chronicle;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class MetaStateToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChronicleMetaState state)
            return Brushes.Transparent;

        // very subtle backgrounds (alpha ~ 8%)
        return state switch
        {
            ChronicleMetaState.Active    => new SolidColorBrush(Color.FromArgb(0x14, 0x22, 0xC5, 0x5E)), // green-ish
            ChronicleMetaState.Due   => new SolidColorBrush(Color.FromArgb(0x14, 0xF9, 0x73, 0x16)), // orange-ish
            ChronicleMetaState.Overdue   => new SolidColorBrush(Color.FromArgb(0x14, 0xEF, 0x44, 0x44)), // red-ish
            ChronicleMetaState.Completed => new SolidColorBrush(Color.FromArgb(0x14, 0x94, 0xA3, 0xB8)), // slate/gray
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class MetaStateToBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChronicleMetaState state)
            return Brushes.Transparent;

        // slightly stronger than background (alpha ~ 19%)
        return state switch
        {
            ChronicleMetaState.Active    => new SolidColorBrush(Color.FromArgb(0x30, 0x22, 0xC5, 0x5E)),
            ChronicleMetaState.Due   => new SolidColorBrush(Color.FromArgb(0x30, 0xF9, 0x73, 0x16)),
            ChronicleMetaState.Overdue   => new SolidColorBrush(Color.FromArgb(0x30, 0xEF, 0x44, 0x44)),
            ChronicleMetaState.Completed => new SolidColorBrush(Color.FromArgb(0x30, 0x94, 0xA3, 0xB8)),
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}