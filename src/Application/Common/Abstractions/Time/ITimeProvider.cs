using NodaTime;

namespace Misa.Application.Common.Abstractions.Time;

public interface ITimeProvider
{
    Instant Now { get; }
}