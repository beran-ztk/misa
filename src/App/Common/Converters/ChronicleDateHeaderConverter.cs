using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Misa.Ui.Avalonia.Common.Converters;

public sealed class ChronicleDateHeaderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime date) return string.Empty;

        var today = DateTime.Today;
        if (date == today)                  return $"Today · {date:dd.MM}";
        if (date == today.AddDays(-1))      return $"Yesterday · {date:dd.MM}";
        if (date == today.AddDays(1))       return $"Tomorrow · {date:dd.MM}";
        return date.ToString("dd.MM · dddd", CultureInfo.CurrentCulture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
