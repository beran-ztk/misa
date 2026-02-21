using System;

namespace Misa.Ui.Avalonia.Common.Converters;

public static class DateTimeOffsetHelper
{
    public static DateTimeOffset? CombineLocalDateAndTimeToUtc(
        DateTimeOffset? date,
        TimeSpan? time)
    {
        if (date is null || time is null)
            return null;

        var localDate = date.Value.Date;
        var localDateTime = localDate + time.Value;

        var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Local);

        return new DateTimeOffset(local).ToUniversalTime();
    }
}