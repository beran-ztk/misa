namespace Misa.Core.Common.Abstractions.Time;

public interface ITimeZoneProvider
{
    bool IsValid(string timeZoneId);
}