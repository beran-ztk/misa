namespace Misa.Application.Common.Abstractions.Time;

public static class TimezoneConversion
{
    public static DateTimeOffset? LocalToUtc(this DateTimeOffset? localDateTime, string zoneId)
        => localDateTime is null ? null : LocalToUtc(localDateTime.Value, zoneId);
    public static DateTimeOffset LocalToUtc(this DateTimeOffset localDateTime, string zoneId)
    {
        if (localDateTime.DateTime.Kind != DateTimeKind.Unspecified)
            localDateTime = DateTime.SpecifyKind(localDateTime.DateTime, DateTimeKind.Unspecified);

        var tz = TimeZoneInfo.FindSystemTimeZoneById(zoneId);

        var offset = tz.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime.DateTime, offset).ToUniversalTime();
    }
    public static DateTimeOffset? UtcToLocal(this DateTimeOffset? utcDateTime, string zoneId)
        => utcDateTime is null ? null : UtcToLocal(utcDateTime.Value, zoneId);
    public static DateTimeOffset UtcToLocal(this DateTimeOffset utcDateTime, string zoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        return TimeZoneInfo.ConvertTime(utcDateTime, tz);
    }
}