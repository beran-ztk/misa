namespace Misa.Core.Abstractions.Time;

public interface ITimeZoneProvider
{
    bool IsValid(string timeZoneId);
}