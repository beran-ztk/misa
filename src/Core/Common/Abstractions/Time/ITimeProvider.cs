using NodaTime;

namespace Misa.Core.Common.Abstractions.Time;

public interface ITimeProvider
{
    Instant Now { get; }
    DateTimeOffset UtcNow { get; }
}