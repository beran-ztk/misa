using NodaTime;

namespace Misa.Application.Abstractions.Time;

public interface ITimeProvider
{
    Instant Now { get; }
    DateTimeOffset UtcNow { get; }
}