namespace Misa.Contract.Features.Authentication;

public sealed record LoginRequestDto(
    string Username,
    string Password
);