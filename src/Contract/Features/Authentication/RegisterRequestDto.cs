namespace Misa.Contract.Features.Authentication;

public sealed record RegisterRequestDto(
    string Email,
    string Username,
    string Password,
    string TimeZone
);