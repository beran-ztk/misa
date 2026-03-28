using NodaTime;

namespace Misa.Core.Abstractions.Time;

public interface ITimeProvider
{
    Instant Now { get; }
    DateTimeOffset UtcNow { get; }
}