namespace Misa.Contract.Features.Authentication;

public sealed record RegisterRequestDto(
    string Username,
    string Password,
    string TimeZone
);