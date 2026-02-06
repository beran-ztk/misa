using Misa.Application.Abstractions.Time;
using NodaTime;

namespace Misa.Infrastructure.Services.Time;

public sealed class TimeProvider : ITimeProvider
{
    public Instant Now => SystemClock.Instance.GetCurrentInstant();
    public DateTimeOffset UtcNow => Now.ToDateTimeOffset();
}