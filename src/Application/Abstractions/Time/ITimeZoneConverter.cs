namespace Misa.Application.Abstractions.Time;

public interface ITimeZoneConverter
{
    DateTimeOffset LocalToUtc(DateTimeOffset localDateTime, string timeZoneId);
    DateTimeOffset? LocalToUtc(DateTimeOffset? localDateTime, string timeZoneId);

    DateTimeOffset UtcToLocal(DateTimeOffset utcDateTime, string timeZoneId);
    DateTimeOffset? UtcToLocal(DateTimeOffset? utcDateTime, string timeZoneId);
}