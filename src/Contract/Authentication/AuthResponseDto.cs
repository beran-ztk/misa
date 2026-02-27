using Misa.Contract.Features.Authentication;

namespace Misa.Contract.Authentication;

public sealed record AuthResponseDto(
    UserDto User
);
public sealed record AuthTokenResponseDto(
    Guid Id,
    string Name,
    string Token
);