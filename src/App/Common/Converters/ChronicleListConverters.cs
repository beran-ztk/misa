using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Misa.Contract.Items.Components.Chronicle;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class ChronicleTypeToChipTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ChronicleEntryType t ? t switch
        {
            ChronicleEntryType.Journal        => "JOURNAL",
            ChronicleEntryType.Deadline       => "DEADLINE",
            ChronicleEntryType.Session        => "SESSION",
            ChronicleEntryType.AuditChange    => "CHANGE",
            ChronicleEntryType.SchedulerEvent => "SCHED",
            _                                 => value.ToString()?.ToUpperInvariant() ?? string.Empty
        } : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ChronicleTypeToChipBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChronicleEntryType t) return Brushes.Transparent;

        return t switch
        {
            ChronicleEntryType.Journal        => new SolidColorBrush(Color.FromArgb(0x14, 0x3B, 0x82, 0xF6)),
            ChronicleEntryType.Deadline       => new SolidColorBrush(Color.FromArgb(0x14, 0xF9, 0x73, 0x16)),
            ChronicleEntryType.Session        => new SolidColorBrush(Color.FromArgb(0x14, 0x22, 0xC5, 0x5E)),
            ChronicleEntryType.AuditChange    => new SolidColorBrush(Color.FromArgb(0x14, 0xA8, 0x55, 0xF7)),
            ChronicleEntryType.SchedulerEvent => new SolidColorBrush(Color.FromArgb(0x14, 0x94, 0xA3, 0xB8)),
            _                                 => Brushes.Transparent
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

        return t switch
        {
            ChronicleEntryType.Journal        => new SolidColorBrush(Color.FromArgb(0x30, 0x3B, 0x82, 0xF6)),
            ChronicleEntryType.Deadline       => new SolidColorBrush(Color.FromArgb(0x30, 0xF9, 0x73, 0x16)),
            ChronicleEntryType.Session        => new SolidColorBrush(Color.FromArgb(0x30, 0x22, 0xC5, 0x5E)),
            ChronicleEntryType.AuditChange    => new SolidColorBrush(Color.FromArgb(0x30, 0xA8, 0x55, 0xF7)),
            ChronicleEntryType.SchedulerEvent => new SolidColorBrush(Color.FromArgb(0x30, 0x94, 0xA3, 0xB8)),
            _                                 => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
