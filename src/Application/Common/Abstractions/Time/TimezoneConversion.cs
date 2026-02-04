namespace Misa.Application.Common.Abstractions.Time;

public static class TimezoneConversion
{
    public static DateTimeOffset? LocalInZoneToUtc(this DateTimeOffset? localDateTime, string zoneId)
        => localDateTime is null ? null : LocalInZoneToUtc(localDateTime.Value, zoneId);
    public static DateTimeOffset LocalInZoneToUtc(this DateTimeOffset localDateTime, string zoneId)
    {
        if (localDateTime.DateTime.Kind != DateTimeKind.Unspecified)
            localDateTime = DateTime.SpecifyKind(localDateTime.DateTime, DateTimeKind.Unspecified);

        var tz = TimeZoneInfo.FindSystemTimeZoneById(zoneId);

        var offset = tz.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime.DateTime, offset).ToUniversalTime();
    }
}