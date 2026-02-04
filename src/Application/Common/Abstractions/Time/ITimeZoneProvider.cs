namespace Misa.Application.Common.Abstractions.Time;

public interface ITimeZoneProvider
{
    bool IsValid(string timeZoneId);
}