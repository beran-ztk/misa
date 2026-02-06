using Misa.Application.Abstractions.Time;

namespace Misa.Infrastructure.Services.Time;

public class TimeZoneConverter : ITimeZoneConverter
{
    public DateTimeOffset? LocalToUtc(DateTimeOffset? localDateTime, string zoneId)
        => localDateTime is null ? null : LocalToUtc(localDateTime.Value, zoneId);
    public DateTimeOffset LocalToUtc(DateTimeOffset localDateTime, string zoneId)
    {
        if (localDateTime.DateTime.Kind != DateTimeKind.Unspecified)
            localDateTime = DateTime.SpecifyKind(localDateTime.DateTime, DateTimeKind.Unspecified);

        var tz = TimeZoneInfo.FindSystemTimeZoneById(zoneId);

        var offset = tz.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime.DateTime, offset).ToUniversalTime();
    }
    public DateTimeOffset? UtcToLocal(DateTimeOffset? utcDateTime, string zoneId)
        => utcDateTime is null ? null : UtcToLocal(utcDateTime.Value, zoneId);
    public DateTimeOffset UtcToLocal(DateTimeOffset utcDateTime, string zoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        return TimeZoneInfo.ConvertTime(utcDateTime, tz);
    }
}