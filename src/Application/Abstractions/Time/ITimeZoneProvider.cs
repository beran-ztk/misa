namespace Misa.Application.Abstractions.Time;

public interface ITimeZoneProvider
{
    bool IsValid(string timeZoneId);
}