using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class UtcToLocalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;

        if (value is DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc)
                return dt.ToLocalTime();

            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
        }

        if (value is DateTimeOffset dto)
            return dto.ToLocalTime();

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return dt.ToUniversalTime();

        if (value is DateTimeOffset dto)
            return dto.ToUniversalTime();

        return value;
    }
}