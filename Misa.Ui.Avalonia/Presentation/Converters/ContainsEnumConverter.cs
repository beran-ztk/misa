using System;
using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Misa.Ui.Avalonia.Presentation.Converters;

public class ContainsEnumConverter : IValueConverter 
{
    public static readonly ContainsEnumConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IList list && parameter != null)
            return list.Contains(parameter);

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return false;
    }
}