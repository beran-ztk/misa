using Misa.Application.Abstractions.Time;
using NodaTime;

namespace Misa.Infrastructure.Services.Time;

public sealed class TimeZoneProvider : ITimeZoneProvider
{
    public bool IsValid(string timeZoneId)
        => DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) is not null;
}