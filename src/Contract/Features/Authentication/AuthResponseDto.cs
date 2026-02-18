namespace Misa.Contract.Features.Authentication;

public sealed record AuthResponseDto(
    UserDto User
);
public sealed record AuthTokenResponseDto(
    UserDto User,
    string Token
);