using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Misa.Contract.Items.Components.Activity.Sessions;

namespace Misa.Ui.Avalonia.Common.Converters;

/// <summary>
/// Converts item CreatedAt DateTimeOffset.
/// Returns "Open for Xd" string when targetType is string/object (for Text binding),
/// or true/false when targetType is bool (for IsVisible binding).
/// Threshold: 45 days.
/// </summary>
public sealed class ItemAgeConverter : IValueConverter
{
    private const int ThresholdDays = 45;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTimeOffset createdAt)
            return targetType == typeof(bool) ? (object)false : null;

        var age = (int)(DateTimeOffset.UtcNow - createdAt).TotalDays;

        if (age < ThresholdDays)
            return targetType == typeof(bool) ? (object)false : null;

        return targetType == typeof(bool) ? (object)true : $"Open for {age}d";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>Converts nullable DateTimeOffset deadline to short date string (e.g. "14 Mar"), or null.</summary>
public sealed class DeadlineShortConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTimeOffset dueAt) return null;
        return dueAt.ToLocalTime().ToString("dd MMM", CultureInfo.InvariantCulture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>Returns true when a non-null deadline is in the past (overdue).</summary>
public sealed class IsOverdueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTimeOffset dueAt && dueAt < DateTimeOffset.UtcNow;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>Returns true when a non-null deadline is within the next 3 days and not yet past.</summary>
public sealed class IsDueSoonConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTimeOffset dueAt) return false;
        var now = DateTimeOffset.UtcNow;
        return dueAt >= now && (dueAt - now).TotalDays <= 3;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
