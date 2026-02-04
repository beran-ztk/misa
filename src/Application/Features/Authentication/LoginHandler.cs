using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Authentication;

namespace Misa.Application.Features.Authentication;
public sealed record LoginCommand(string Username, string Password);

public sealed class LoginHandler(IAuthenticationRepository repository)
{
    public async Task<Result<AuthResponseDto>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var username = cmd.Username.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cmd.Password))
            return Result<AuthResponseDto>.Conflict("","Username und Password dürfen nicht leer sein.");

        var user = await repository.FindByUsernameAndPasswordAsync(username, cmd.Password, ct);
        if (user is null)
            return Result<AuthResponseDto>.Conflict("","Ungültige Zugangsdaten.");

        var dto = new UserDto(user.Id, user.Username, user.TimeZone);
        return Result<AuthResponseDto>.Ok(new AuthResponseDto(dto));
    }
}