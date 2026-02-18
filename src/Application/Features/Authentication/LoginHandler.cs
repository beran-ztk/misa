using Misa.Application.Abstractions.Authentication;
using Misa.Contract.Features.Authentication;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Authentication;

public sealed record LoginCommand(string Username, string Password);

public sealed class LoginHandler(IIdentityAuthStore authStore)
{
    public async Task<Result<AuthTokenResponseDto>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var username = cmd.Username.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cmd.Password))
            return Result<AuthTokenResponseDto>.Conflict("", "Username and password must not be empty.");

        var login = await authStore.LoginAsync(username, cmd.Password, ct);

        if (!login.IsSuccess)
            return Result<AuthTokenResponseDto>.Conflict("", login.Error?.Message ?? "Invalid credentials.");

        var value = login.Value!;

        var dto = new UserDto(value.UserId, value.Username, string.Empty);

        return Result<AuthTokenResponseDto>.Ok(
            new AuthTokenResponseDto(dto, value.Token));
    }
}