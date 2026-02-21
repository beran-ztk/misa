using Misa.Application.Abstractions.Authentication;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Authentication;

namespace Misa.Application.Features.Authentication;

public sealed record LoginCommand(string Username, string Password);

public sealed class LoginHandler(IIdentityAuthStore authStore)
{
    public async Task<AuthTokenResponseDto> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var username = cmd.Username.Trim();

        var login = await authStore.LoginAsync(username, cmd.Password, ct);

        var value = login.Value!;

        return new AuthTokenResponseDto(value.UserId, value.Username, value.Token);
    }
}