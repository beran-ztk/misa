namespace Misa.Contract.Features.Authentication;

public sealed record UserDto(
    Guid Id,
    string Username,
    string TimeZone
);